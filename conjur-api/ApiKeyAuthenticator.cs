// <copyright file="ApiKeyAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     API key authenticator.
// </summary>

using System.Runtime.InteropServices;

namespace Conjur;

/// <summary>
/// API key authenticator.
/// </summary>
public class ApiKeyAuthenticator : IAuthenticator
{
    private readonly Uri uri;
    private readonly NetworkCredential credential;
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
    private readonly Client client;

    private volatile string token;
    private Timer timer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Conjur.ApiKeyAuthenticator"/> class.
    /// </summary>
    /// <param name="authnUri">Authentication base URI, for example
    /// "https://example.com/api/authn".</param>
    /// <param name="account">The name of the Conjur organization account.</param>
    /// <param name="credential">Username and API key to use, where
    /// username is for example "bob" or "host/jenkins".</param>
    /// <param name="client">Client to use</param>
    public ApiKeyAuthenticator(Uri authnUri, string account, NetworkCredential credential, Client client)
    {
        uri = new Uri($"{authnUri}/{Uri.EscapeDataString(account)}/{Uri.EscapeDataString(credential.UserName)}/authenticate");
        this.credential = credential;
        this.client = client;
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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            var bStr = IntPtr.Zero;
            var bArr = new byte[credential.SecurePassword.Length];
            try
            {
                bStr = Marshal.SecureStringToBSTR(credential.SecurePassword);
                for (var i = 0; i < credential.SecurePassword.Length; i++)
                {
                    bArr[i] = Marshal.ReadByte(bStr, i * 2);
                }

                using var memoryStream = new MemoryStream();
                memoryStream.Write(bArr, 0, bArr.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var stream = new StreamContent(memoryStream))
                {
                    stream.Headers.ContentLength = credential.SecurePassword.Length;
                    httpRequestMessage.Content = stream;

                    localToken = client.Send(httpRequestMessage).Read();
                    Interlocked.Exchange(ref token, localToken);
                }

                StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
            }
            finally
            {
                if (bStr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bStr);
                }

                Array.Clear(bArr, 0, bArr.Length);
            }
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

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            var bStr = IntPtr.Zero;
            var bArr = new byte[credential.SecurePassword.Length];
            try
            {
                bStr = Marshal.SecureStringToBSTR(credential.SecurePassword);
                for (var i = 0; i < credential.SecurePassword.Length; i++)
                {
                    bArr[i] = Marshal.ReadByte(bStr, i * 2);
                }

                using var memoryStream = new MemoryStream();
                memoryStream.Write(bArr, 0, bArr.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var stream = new StreamContent(memoryStream))
                {
                    stream.Headers.ContentLength = credential.SecurePassword.Length;
                    httpRequestMessage.Content = stream;

                    localToken = await client.SendAsync(httpRequestMessage, cancellationToken).ReadAsync(cancellationToken);
                    Interlocked.Exchange(ref token, localToken);
                }

                StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
            }
            finally
            {
                if (bStr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bStr);
                }

                Array.Clear(bArr, 0, bArr.Length);
            }
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
        var authenticator = (ApiKeyAuthenticator)state;
        // timer is disposable resource but there is no way to dispose it from outside
        // so each time when token expires we dispose it
        // will allow garbage collection of necessary client and authenticator classes
        authenticator.timer.Dispose();
        authenticator.timer = null;
        Interlocked.Exchange(ref authenticator.token, null);
    }
}
