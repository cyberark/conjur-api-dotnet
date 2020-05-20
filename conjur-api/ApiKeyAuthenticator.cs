// <copyright file="ApiKeyAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     API key authenticator.
// </summary>

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Conjur
{
    /// <summary>
    /// API key authenticator.
    /// </summary>
    public class ApiKeyAuthenticator : IAuthenticator
    {
        private readonly Uri uri;
        private readonly NetworkCredential credential;
        private readonly object locker = new object();

        private string token = null;
        private Timer timer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.ApiKeyAuthenticator"/> class.
        /// </summary>
        /// <param name="authnUri">Authentication base URI, for example
        /// "https://example.com/api/authn".</param>
        /// <param name="account">The name of the Conjur organization account.</param>
        /// <param name="credential">User name and API key to use, where
        /// username is for example "bob" or "host/jenkins".</param>
        public ApiKeyAuthenticator(Uri authnUri, string account, NetworkCredential credential)
        {
            this.credential = credential;
            this.uri = new Uri($"{authnUri}/{Uri.EscapeDataString(account)}/{Uri.EscapeDataString(credential.UserName)}/authenticate");
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Obtain a Conjur authentication token.
        /// </summary>
        /// <returns>Conjur authentication token in verbatim form.
        /// It needs to be base64-encoded to be used in a web request.</returns>
        public string GetToken()
        {
            string token = this.token;
            if (token != null)
            {
                return token;
            }

            lock (this.locker)
            {
                if (this.token == null)
                {
                    HttpWebRequest request = WebRequest.CreateHttp(this.uri);
                    request.Timeout = ApiConfigurationManager.GetInstance().HttpRequestTimeout;
                    request.Method = WebRequestMethods.Http.Post;
                    request.ContentLength = credential.SecurePassword.Length;
                    request.AllowWriteStreamBuffering = false;

                    IntPtr bstr = IntPtr.Zero;
                    byte[] bArr = new byte[credential.SecurePassword.Length];
                    try
                    {
                        bstr = Marshal.SecureStringToBSTR(credential.SecurePassword);
                        for (int i = 0; i < credential.SecurePassword.Length; i++)
                        {
                            bArr[i] = Marshal.ReadByte(bstr, i * 2);
                        }
                        using (Stream stream = request.GetRequestStream())
                        {
                            stream.Write(bArr, 0, bArr.Length);
                            Interlocked.Exchange(ref this.token, request.Read());
                            this.StartTokenTimer(TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().TokenRefreshTimeout));
                        }
                    }
                    finally
                    {
                        if (bstr != IntPtr.Zero)
                        {
                            Marshal.ZeroFreeBSTR(bstr);
                        }
                        Array.Clear(bArr, 0, bArr.Length);
                    }
                }
            }
            return this.token;
        }
        #endregion

        internal void StartTokenTimer(TimeSpan timeout)
        {
            this.timer = new Timer(this.TimerCallback, null, timeout, Timeout.InfiniteTimeSpan);
        }

        private void TimerCallback(object state)
        {
            // timer is disposable resource but there is no way to dispose it from outside
            // so each time when token expires we dispose it
            // it will allow garbage collection of unecessary client and authentificator classes
            this.timer.Dispose();
            this.timer = null;
            Interlocked.Exchange(ref this.token, null);
        }
    }
}
