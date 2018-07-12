// <copyright file="ApiKeyAuthenticator.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     API key authenticator.
// </summary>

namespace Conjur
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;

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
            this.uri = new Uri($"{authnUri}/{WebUtility.UrlEncode(account)}/{WebUtility.UrlEncode(credential.UserName)}/authenticate");
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
                    WebRequest request = WebRequest.Create(this.uri);
                    request.Method = WebRequestMethods.Http.Post;

                    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(this.credential.Password);
                    }

                    Interlocked.Exchange(ref this.token, request.Read());
                    this.StartTokenTimer(new TimeSpan(0, 7, 30));
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
