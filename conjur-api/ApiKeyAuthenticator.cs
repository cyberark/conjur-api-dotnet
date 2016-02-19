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
    using System.Text;

    /// <summary>
    /// API key authenticator.
    /// </summary>
    public class ApiKeyAuthenticator : IAuthenticator
    {
        private readonly Uri uri;
        private readonly string apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.ApiKeyAuthenticator"/> class.
        /// </summary>
        /// <param name="authnUri">Authentication base URI, for example 
        /// "https://example.com/api/authn".</param>
        /// <param name="userName">Authn user name, such as "bob" or "host/jenkins".</param>
        /// <param name="apiKey">API key of that user.</param>
        public ApiKeyAuthenticator(Uri authnUri, string userName, string apiKey)
        {
            this.uri = new Uri(authnUri + "/users/" + WebUtility.UrlEncode(userName)
                + "/authenticate");
            this.apiKey = apiKey;
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Apply the authentication to a WebRequest.
        /// </summary>
        /// <param name="webRequest">Web request to apply the authentication to.</param>
        public void Apply(System.Net.HttpWebRequest webRequest)
        {
            webRequest.Headers["Authorization"] = "Token token=\""
            + this.Base64Token() + "\"";
        }

        #endregion

        private string Base64Token()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Token()));
        }

        private string Token()
        {
            // TODO: reuse token until it expires
            var request = WebRequest.Create(this.uri);
            request.Method = "POST";

            var stream = request.GetRequestStream();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(this.apiKey);
            }

            return new StreamReader(request.GetResponse().GetResponseStream())
                .ReadToEnd();
        }
    }
}
