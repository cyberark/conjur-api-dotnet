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
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrEmpty(serviceId))
            throw new ArgumentException("serviceId must not be null or empty.", nameof(serviceId));

        this.client = client;
        HostId = hostId;

        // Build the URI for the cert authentication endpoint
        // authn-cert/<service-id>/<account>[/<host-id>]/authenticate
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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            };

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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            };

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

    internal void StartTokenTimer(TimeSpan timeout)
    {
        timer = new Timer(TimerCallback, this, timeout, Timeout.InfiniteTimeSpan);
    }

    private static void TimerCallback(object state)
    {
        var authenticator = (CertAuthenticator)state;
        // timer is disposable resource but there is no way to dispose it from outside
        // so each time when token expires we dispose it
        // will allow garbage collection of necessary client and authenticator classes
        authenticator.timer.Dispose();
        authenticator.timer = null;
        Interlocked.Exchange(ref authenticator.token, null);
    }
}

