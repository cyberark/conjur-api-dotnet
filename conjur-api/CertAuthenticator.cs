// <copyright file="CertAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Certificate-based authenticator.
// </summary>

namespace Conjur;

/// <summary>
/// Certificate-based authenticator for mTLS authentication using authn-cert.
/// </summary>
public class CertAuthenticator : IAuthenticator
{
    private readonly Uri uri;
    private readonly Client client;
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    private volatile string token;
    private readonly object timerLock = new();
    private Timer timer;

    /// <summary>
    /// Gets or sets the Conjur host ID for request mode authentication.
    /// Leave empty for SPIFFE mode where the host is derived from the certificate's SPIFFE SAN URI.
    /// </summary>
    public string HostId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Conjur.CertAuthenticator"/> class.
    /// </summary>
    /// <param name="client">The Conjur client to use for authentication.</param>
    /// <param name="serviceId">The service ID for the cert authenticator (e.g., "my-service").</param>
    /// <param name="hostId">The host ID for request mode (optional; leave empty for SPIFFE mode).</param>
    public CertAuthenticator(Client client, string serviceId, string hostId = "")
    {
        this.client = client;
        HostId = hostId;

        // Build the URI for the cert authentication endpoint
        // authn-cert/<service-id>/<account>[/<host-id>]/authenticate
        // Host IDs use EscapeDataString (encodes '/' as %2F), matching Go url.PathEscape
        // and Conjur authn-cert docs (e.g. host%2Fvm-workloads%2Fvm-01).
        if (!string.IsNullOrEmpty(hostId))
        {
            uri = new Uri($"{client.ApplianceUri}authn-cert/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(client.AccountName)}/{Uri.EscapeDataString(hostId)}/authenticate");
        }
        else
        {
            // SPIFFE mode - no host ID in the URL
            uri = new Uri($"{client.ApplianceUri}authn-cert/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(client.AccountName)}/authenticate");
        }
    }

    #region IAuthenticator implementation

    public string GetToken()
    {
        var localToken = token;
        if (localToken is not null)
        {
            return localToken;
        }

        semaphoreSlim.Wait();
        try
        {
            localToken = token;
            if (localToken is not null)
            {
                return localToken;
            }

            var httpRequestMessage = CreateAuthenticateRequest();

            localToken = client.Send(httpRequestMessage).Read();
            Interlocked.Exchange(ref token, localToken);

            StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return localToken;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var localToken = token;
        if (localToken is not null)
        {
            return localToken;
        }

        await semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            localToken = token;
            if (localToken is not null)
            {
                return localToken;
            }

            var httpRequestMessage = CreateAuthenticateRequest();

            localToken = await client.SendAsync(httpRequestMessage, cancellationToken).ReadAsync(cancellationToken);
            Interlocked.Exchange(ref token, localToken);

            StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return localToken;
    }

    #endregion

    private HttpRequestMessage CreateAuthenticateRequest() =>
        new(HttpMethod.Post, uri)
        {
            // authn-cert uses mTLS for credentials; body must be empty (Go SDK parity).
            Content = new ByteArrayContent(Array.Empty<byte>())
        };

    internal void StartTokenTimer(TimeSpan timeout)
    {
        Timer newTimer = null!;
        newTimer = new Timer(_ => OnTokenExpired(newTimer), null, timeout, Timeout.InfiniteTimeSpan);

        lock (timerLock)
        {
            timer?.Dispose();
            timer = newTimer;
        }
    }

    private void OnTokenExpired(Timer expiredTimer)
    {
        lock (timerLock)
        {
            if (!ReferenceEquals(timer, expiredTimer))
                return;

            timer.Dispose();
            timer = null;
        }

        Interlocked.Exchange(ref token, null);
    }
}

