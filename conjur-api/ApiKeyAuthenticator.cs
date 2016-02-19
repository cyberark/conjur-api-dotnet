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
        private readonly Uri uri;
        private readonly NetworkCredential credential;

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
            // TODO: reuse token until it expires
            var request = WebRequest.Create(this.uri);
            request.Method = "POST";

            var stream = request.GetRequestStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(this.credential.Password);
            }

            return new StreamReader(request.GetResponse().GetResponseStream())
                .ReadToEnd();
        }

        #endregion
    }
}
