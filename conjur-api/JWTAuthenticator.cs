// <copyright file="JWTAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     JWT authenticator.
// </summary>

namespace Conjur;

/// <summary>
/// JWT authenticator.
/// </summary>
public class JWTAuthenticator(Client client, IJWTProvider jwtProvider, string serviceId, string hostId = null, object providerData = null) : IAuthenticator
{
    private readonly Uri uri = BuildUri(client, serviceId, hostId);
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    private volatile string token;
    private Timer timer;

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

            var jwtToken = jwtProvider.GetJWT(providerData);
            using var request = BuildRequestMessage(jwtToken);

            localToken = client.Send(request).Read();
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

            var jwtToken = await jwtProvider.GetJWTAsync(providerData, cancellationToken);
            using var request = BuildRequestMessage(jwtToken);

            localToken = await client.SendAsync(request, cancellationToken).ReadAsync(cancellationToken);
            Interlocked.Exchange(ref token, localToken);

            StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
        }
        finally
        {
            semaphoreSlim.Release();
        }

        return localToken;
    }

    private HttpRequestMessage BuildRequestMessage(string jwtToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("jwt", jwtToken)])
        };
        return request;
    }

    internal void StartTokenTimer(TimeSpan timeout)
    {
        timer = new Timer(TimerCallback, this, timeout, Timeout.InfiniteTimeSpan);
    }

    private static void TimerCallback(object state)
    {
        var authenticator = (JWTAuthenticator)state;
        // timer is disposable resource but there is no way to dispose it from outside
        // so each time when token expires we dispose it
        // will allow garbage collection of necessary client and authenticator classes
        authenticator.timer.Dispose();
        authenticator.timer = null;
        Interlocked.Exchange(ref authenticator.token, null);
    }

    private static Uri BuildUri(Client client, string serviceId, string hostId) =>
        string.IsNullOrEmpty(hostId)
            ? new Uri($"{client.ApplianceUri}authn-jwt/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(client.AccountName)}/authenticate")
            : new Uri($"{client.ApplianceUri}authn-jwt/{Uri.EscapeDataString(serviceId)}/{Uri.EscapeDataString(client.AccountName)}/{Uri.EscapeDataString(hostId)}/authenticate");
}
