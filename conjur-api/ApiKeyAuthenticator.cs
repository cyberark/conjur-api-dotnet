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

        private string token;
        private Timer timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.ApiKeyAuthenticator"/> class.
        /// </summary>
        /// <param name="authnUri">Authentication base URI, for example 
        /// "https://example.com/api/authn".</param>
        /// <param name="credential">User name and API key to use, where 
        /// username is for example "bob" or "host/jenkins".</param>
        public ApiKeyAuthenticator(Uri authnUri, NetworkCredential credential)
        {
            this.credential = credential;
            this.uri = new Uri(authnUri + "/users/"
                + WebUtility.UrlEncode(credential.UserName)
                + "/authenticate");
            this.timer = new Timer((_) => Interlocked.Exchange(ref this.token, null));
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Obtain a Conjur authentication token.
        /// </summary>
        /// <returns>Conjur authentication token in verbatim form.
        /// It needs to be base64-encoded to be used in a web request.</returns>
        public string GetToken()
        {
            var token = this.token;
            if (token != null)
                return token;

            lock(timer) if (this.token == null) {
                var request = WebRequest.Create(this.uri);
                request.Method = "POST";

                using (var writer = new StreamWriter(request.GetRequestStream()))
                    writer.Write(this.credential.Password);

                Interlocked.Exchange(ref this.token, request.Read());
                this.StartTokenTimer(new TimeSpan(0, 7, 30));
            }

            return this.token;
        }

        #endregion

        internal void StartTokenTimer(TimeSpan timeout)
        {
            this.timer.Change(timeout, Timeout.InfiniteTimeSpan);
        }
    }
}
