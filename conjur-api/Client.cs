// <copyright file="Client.cs" company="Conjur Inc.">
//     Copyright (c) 2016-2018 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Base Conjur client class implementation.
// </summary>

namespace Conjur
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Runtime.Serialization;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Conjur API client.
    /// </summary>
    public partial class Client
    {
        private Uri applianceUri;
        private string account;
        private bool urlValidated = false;
        private string token;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Client"/> class.
        /// </summary>
        /// <param name="applianceUri">Appliance URI.</param>
        public Client(string applianceUri)
        {
            this.applianceUri = NormalizeBaseUri(applianceUri);
            this.TrustedCertificates = new X509Certificate2Collection();
        }

        /// <summary>
        /// Switch the client to ActingAs another role. Set to null by default.
        /// </summary>
        /// Note support for this value is limited in the current version of this library.
        /// <value>Fully qualified role name. For example MyCompanyName:group:security_admin.</value>
        public string ActingAs { get; set; }

        /// <summary>
        /// Gets the appliance URI.
        /// </summary>
        /// <value>The appliance URI.</value>
        public Uri ApplianceUri
        {
            get
            {
                return this.applianceUri;
            }
        }

        /// <summary>
        /// Gets or sets the authenticator used to establish Conjur identity.
        /// This gets automatically set by setting <see cref="Client.Credential"/>.
        /// </summary>
        /// <value>The authenticator.</value>
        public IAuthenticator Authenticator
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the username and API key to authenticate.
        /// This initializes <see cref="Client.Authenticator"/>.
        /// Use <see cref="Client.LogIn"/> to use a password.
        /// </summary>
        /// <value>The credential of user name and API key, where user name is
        /// for example "bob" or "host/jenkins".</value>
        public NetworkCredential Credential
        {
            set
            {
                this.Authenticator = new ApiKeyAuthenticator(
                    new Uri(this.ValidateBaseUri() + "authn"), 
                    value);
            }
        }

        /// <summary>
        /// Gets the collection of extra certificates trusted for authenticating the
        /// Conjur server in addition to the system ones.
        /// </summary>
        /// <value>The trusted certificates collection.</value>
        public X509Certificate2Collection TrustedCertificates
        {
            get;
        }

        /// <summary>
        /// Gets the name of the Conjur organization account.
        /// </summary>
        /// <returns>The account name.</returns>
        public string GetAccountName()
        {
            if (this.account == null)
                this.account = this.Info().account;
            return this.account;
        }

        /// <summary>
        /// Logs in using a password. Sets <see cref="Authenticator"/>
        /// <seealso cref="Credential"/>
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="userName">User name to log in as (for example "bob"
        /// or "host/example.com".</param>
        /// <param name="password">Password of the user.</param>
        public void LogIn(string userName, string password, string account)
        {
            this.account = account;

            var apiKey = this.LogIn(new NetworkCredential(userName, password), account);
            this.Authenticate(new NetworkCredential(userName, apiKey), account);
        }

        /// <summary>
        /// Logs in using a password. Sets <see cref="Authenticator"/>
        /// <seealso cref="Credential"/>
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="credential">The credential of user name and password, 
        /// where user name is for example "bob" or "host/jenkins".</param>
        public string LogIn(NetworkCredential credential, string account)
        {
            var wr = this.Request(String.Format("authn/{0}/login", account));
            wr.Method = "GET";
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                               credential.UserName + ":" + credential.Password));

            wr.Headers["Authorization"] = "Basic " + auth;
            var apiKey = wr.Read();

            this.Credential = new NetworkCredential(credential.UserName, apiKey);

            return apiKey;
        }

        /// <summary>
        /// Authenticates the user/host using their API key. Sets <see cref="token"/> and <see cref="Credential"/>
        /// <seealso cref="Credential"/>
        /// </summary>
        /// <returns>nothing</returns>
        /// <param name="credential">The credential of user name and password,
        /// where user name is for example "bob" or "host/jenkins".</param>
        /// <param name="account">The account of the user/host.</param>
        public void Authenticate(NetworkCredential credential, string account)
        {
            var wr = this.Request(String.Format("authn/{0}/{1}/authenticate", account, credential.UserName));
            wr.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(credential.Password);
            wr.ContentType = "application/json; charset=utf-8";
            var dataStream = wr.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            this.token = Convert.ToBase64String(Encoding.UTF8.GetBytes(wr.Read()));

            this.Credential = new NetworkCredential(credential.UserName, credential.Password);
        }

        /// <summary>
        /// Create a WebRequest for the specified path.
        /// </summary>
        /// <param name="path">Path, NOT including the leading slash.</param>
        /// <returns>A WebRequest for the specified appliance path.</returns>
        public WebRequest Request(string path)
        {
            return WebRequest.Create(this.ValidateBaseUri() + path);
        }

        /// <summary>
        /// Create an authenticated WebRequest for the specified path.
        /// </summary>
        /// <param name="path">Path, NOT including the leading slash.</param>
        /// <returns>A WebRequest for the specified appliance path, with 
        /// authorization header set using <see cref="Authenticator"/>.</returns>
        public WebRequest AuthenticatedRequest(string path)
        {
            return this.ApplyAuthentication(this.Request(path));
        }

        /// <summary>
        /// Validates the appliance base URI.
        /// Tries to connect to /info; if not successful, try again adding an /api prefix.
        /// Also sets up certificate validation.
        /// </summary>
        /// <returns>The validated base appliance URI.</returns>
        public Uri ValidateBaseUri()
        {
            if (!this.urlValidated)
            {
                // TODO: figure out how to avoid changing the default for all hosts
                ServicePointManager.ServerCertificateValidationCallback = 
                    new RemoteCertificateValidationCallback(this.ValidateCertificate);

                var wr = WebRequest.Create(this.applianceUri + "info");
                wr.Method = "HEAD";
                try
                {
                    wr.GetResponse().Close();
                }
                catch (WebException)
                {   // TODO: deprecated, in need of refactor.
                    // forgotten /api at the end of the Uri? Try again.
                    this.applianceUri = new Uri(this.applianceUri + "api/");
                    wr = WebRequest.Create(this.applianceUri + "info");
                    wr.Method = "HEAD";
                    wr.GetResponse().Close();
                }

                this.urlValidated = true;
            }

            return this.applianceUri;
        }

        /// <summary>
        /// Normalizes the base URI, removing double slashes and adding a trailing
        /// slash, as necessary.
        /// </summary>
        /// <returns>The normalized base URI.</returns>
        /// <param name="uri">Base appliance URI to normalize.</param>
        private static Uri NormalizeBaseUri(string uri)
        {
            // appliance's nginx doesn't like double slashes,
            // eg. it returns 401 on https://example.org//api/info

            // so normalize to remove double slashes
            var normalizedUri = Regex.Replace(uri, "(?<!:)/+", "/");

            // make sure there is a trailing slash
            return new Uri(Regex.Replace(normalizedUri, "(?<!/)\\z", "/"));
        }

        /// <summary>
        /// Validates the Conjur appliance certificate. 
        /// <see cref="RemoteCertificateValidationCallback"/> 
        /// </summary>
        /// <returns><c>true</c>, if certificate was valid, <c>false</c> otherwise.</returns>
        /// <param name="sender">Sender of the validation request.</param>
        /// <param name="certificate">Certificate to be validated.</param>
        /// <param name="chain">Certificate chain, as resolved by the system.</param>
        /// <param name="sslPolicyErrors">SSL policy errors from the system.</param>
        private bool ValidateCertificate(
            object sender, 
            X509Certificate certificate, 
            X509Chain chain, 
            SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    return chain.VerifyWithExtraRoots(certificate, this.TrustedCertificates);
                case SslPolicyErrors.None:
                    return true;
                default:
                    return false;
            }
        }

        private WebRequest ApplyAuthentication(WebRequest webRequest)
        {
            if (this.Authenticator == null)
                throw new InvalidOperationException("Authentication required.");

            webRequest.Headers["Authorization"] = "Token token=\"" + token + "\"";
            return webRequest;
        }

        /// <summary>
        /// Get the server info.
        /// </summary>
        /// <returns>Server information.</returns>
        private ServerInfo Info()
        {
            return JsonSerializer<ServerInfo>.Read(this.Request("info"));
        }

        [DataContract]
        internal class ServerInfo
        {
            [DataMember]
            internal string account;
        }
    }
}
