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

    /// <summary>
    /// API key authenticator.
    /// </summary>
    public class ApiKeyAuthenticator : IAuthenticator
    {
        internal class Token
        {
            private readonly DateTime expiration;
            public string Value { get; }

            public Token(string value, TimeSpan validPeriod)
            {
                Value = value;
                expiration = DateTime.Now.Add(validPeriod);
            }

            public bool Expired()
            {
                return DateTime.Now > expiration;
            }
        }

        private readonly Uri uri;
        private readonly NetworkCredential credential;
        private readonly object lockObject = new object();

        private Token token = null;

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
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Obtain a Conjur authentication token.
        /// </summary>
        /// <returns>Conjur authentication token in verbatim form.
        /// It needs to be base64-encoded to be used in a web request.</returns>
        public string GetToken()
        {
            if (this.token == null || this.token.Expired())
            {
                lock (lockObject)
                {
                    if (this.token == null || this.token.Expired())
                    {
                        WebRequest request = WebRequest.Create(this.uri);
                        request.Method = "POST";

                        using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                        {
                            writer.Write(this.credential.Password);
                        }

                        this.token = new Token(request.Read(), new TimeSpan(0, 7, 30));
                    }
                }
            }

            return this.token.Value;
        }

        #endregion
    }
}
