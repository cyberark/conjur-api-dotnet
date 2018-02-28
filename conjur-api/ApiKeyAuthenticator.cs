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
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        // NOTE: since the timer executes on a different
        // thread we cannot use token == null, but need
        // the extra boolean
        private string token;
        private bool tokenExpired = true;
        private Timer timer;

        private bool TokenExpired 
        {
            get
            {
                rwLock.EnterReadLock();
                try
                {
                    return tokenExpired;
                }
                finally
                {
                    rwLock.ExitReadLock();               
                }
            }
        }

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
            this.timer = new Timer((_) => 
            {
                rwLock.EnterWriteLock();
                this.tokenExpired = true;
                rwLock.ExitWriteLock();
            });
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Obtain a Conjur authentication token.
        /// </summary>
        /// <returns>Conjur authentication token in verbatim form.
        /// It needs to be base64-encoded to be used in a web request.</returns>
        public string GetToken()
        {
            if (this.TokenExpired)
            {
                rwLock.EnterWriteLock();

                try
                {
                    RenewToken();
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            return this.token;
        }

        #endregion

        private void RenewToken()
        {
            var request = WebRequest.Create(this.uri);
            request.Method = "POST";

            using (var writer = new StreamWriter(request.GetRequestStream()))
                writer.Write(this.credential.Password);

            this.token = request.Read();
            this.StartTokenTimer(new TimeSpan(0, 7, 30));
        }
        
        internal void StartTokenTimer(TimeSpan timeout)
        {
            this.tokenExpired = false;
            this.timer.Change(timeout, Timeout.InfiniteTimeSpan);
        }
    }
}
