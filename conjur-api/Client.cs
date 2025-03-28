// <copyright file="Client.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Base Conjur client class implementation.
// </summary>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Conjur
{
    /// <summary>
    /// Conjur API client.
    /// </summary>
    public partial class Client
    {
        private readonly string account;
        private readonly string actingAs;
        private bool disableCertCheck = false;
        internal HttpClient httpClient;
        private static string integrationName = "SecretsManagerDotNet SDK";
        private static string integrationType = "cybr-secretsmanager";
        private static string integrationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static string vendorName = "CyberArk";

        private static string vendorVersion = null;
        private static string telemetryHeader = null;

        /// <summary>
        /// Gets or sets the name of the integration. 
        /// When set, it also resets the cached telemetry header so it can be rebuilt.
        /// </summary>
        /// <value>
        /// The name of the integration.
        /// </value>
        public static string IntegrationName
        {
            get => integrationName;
            set
            {
                integrationName = value;
                telemetryHeader = null;  
            }
        }

        /// <summary>
        /// Gets or sets the type of the integration. 
        /// When set, it also resets the cached telemetry header so it can be rebuilt.
        /// </summary>
        /// <value>
        /// The type of the integration.
        /// </value>
        public static string IntegrationType
        {
            get => integrationType;
            set
            {
                integrationType = value;
                telemetryHeader = null;  
            }
        }

        /// <summary>
        /// Gets or sets the version of the integration.
        /// When set, it also resets the cached telemetry header so it can be rebuilt.
        /// </summary>
        /// <value>
        /// The version of the integration.
        /// </value>
        public static string IntegrationVersion
        {
            get => integrationVersion;
            set
            {
                integrationVersion = value;
                telemetryHeader = null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the vendor.
        /// When set, it also resets the cached telemetry header so it can be rebuilt.
        /// </summary>
        /// <value>
        /// The name of the vendor.
        /// </value>
        public static string VendorName
        {
            get => vendorName;
            set
            {
                vendorName = value;
                telemetryHeader = null;
            }
        }

        /// <summary>
        /// Gets or sets the version of the vendor.
        /// When set, it also resets the cached telemetry header so it can be rebuilt.
        /// </summary>
        /// <value>
        /// The version of the vendor.
        /// </value>
        public static string VendorVersion
        {
            get => vendorVersion;
            set
            {
                vendorVersion = value;
                telemetryHeader = null;
            }
        }

        /// <summary>
        /// Constructs and returns the telemetry header string based on the integration and vendor information.
        /// The header is encoded in Base64 and uses URL-safe characters (`-` and `_`).
        /// If the telemetry header has been previously generated, it will be cached and reused.
        /// </summary>
        /// <returns>
        /// A Base64 encoded string representing the telemetry header, formatted as a URL-safe string.
        /// </returns>
        public static string GetTelemetryHeader(){
            var sb = new StringBuilder();
            if( telemetryHeader != null )
            {
                return telemetryHeader;
            }
            telemetryHeader = "";
            if( integrationName != null )
            {
                sb.Append("in=").Append(integrationName);
                if (integrationType != null) 
                {
                    sb.Append("&it=").Append(integrationType); 
                }
                if (integrationVersion != null) 
                {
                    sb.Append("&iv=").Append(integrationVersion); 
                }
            }
            if( vendorName != null )
            {
                sb.Append("&vn=").Append(vendorName);
                if (vendorVersion != null) 
                {
                    sb.Append("&vv=").Append(vendorVersion); 
                }
            }
            telemetryHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return telemetryHeader;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Conjur.Client"/> class.
        /// </summary>
        /// <param name="applianceUri">Appliance URI.</param>
        /// <param name="account">Conjur account.</param>
        public Client(string applianceUri, string account)
        {
            this.account = account;
            this.ApplianceUri = NormalizeBaseUri(applianceUri);
            this.TrustedCertificates = new X509Certificate2Collection();

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = this.ValidateCertificate;

            this.httpClient = new HttpClient(httpClientHandler);
            this.httpClient.DefaultRequestHeaders.Add("x-cybr-telemetry", GetTelemetryHeader());
            this.httpClient.Timeout = TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().HttpRequestTimeout);
        }

        internal Client(Client other, string role) : this(other.ApplianceUri.AbsoluteUri, other.account)
        {
            this.actingAs = role;
            this.httpClient = other.httpClient;
            this.Authenticator = other.Authenticator;
        }

        /// <summary>
        /// Disables SSL Cert check. Can be used when Conjur is configured with self-signed cert.
        /// </summary>
        /// <remarks>
        /// Warning: this is a security risk and should be used only for testing purposes.
        /// </remarks>
        public void DisableCertCheck()
        {
            this.disableCertCheck = true;
        }

        /// <summary>
        /// Enables SSL Cert check. This is already the default. This method is only necessary
        /// if <see cref="DisableCertCheck"/> was called before.
        /// </summary>
        public void EnableCertCheck()
        {
            this.disableCertCheck = false;
        }

        /// <summary>
        /// Gets the appliance URI.
        /// </summary>
        /// <value>The appliance URI.</value>
        public Uri ApplianceUri { get; }

        /// <summary>
        /// Gets or sets the authenticator used to establish Conjur identity.
        /// This gets automatically set by setting <see cref="Client.Credential"/>.
        /// </summary>
        /// <value>The authenticator.</value>
        public IAuthenticator Authenticator { get; set; }

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
                  new Uri(this.ApplianceUri + "authn"),
                  this.GetAccountName(),
                  value,
                  this.httpClient);
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
        public string LogIn(string userName, string password)
        {
            return this.LogIn(new NetworkCredential(userName, password));
        }

        /// <summary>
        /// Logs in using a password. Sets <see cref="Authenticator"/>
        /// <seealso cref="Credential"/>
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="credential">The credential of user name and password,
        /// where user name is for example "bob" or "host/jenkins".</param>
        public string LogIn(NetworkCredential credential)
        {
            var request = this.Request($"authn/{this.account}/login");

            // there seems to be no sane way to force HttpRequestMessage to authenticate
            // properly by itself, so generate the header manually
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential.UserName + ":" + credential.Password));
            request.Headers.Add("Authorization", "Basic " + auth);
            string apiKey = httpClient.Send(request).Read();

            this.Credential = new NetworkCredential(credential.UserName, apiKey);
            return apiKey;
        }

        /// <summary>
        /// Create an HttpRequestMessage for the specified path.
        /// </summary>
        /// <param name="path">Path, NOT including the leading slash.</param>
        /// <returns>An HttpRequestMessage for the specified appliance path.</returns>
        public HttpRequestMessage Request(string path)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.RequestUri = new Uri(this.ApplianceUri + path);
            return httpRequestMessage;
        }

        /// <summary>
        /// Create an authenticated HttpRequestMessage for the specified path.
        /// </summary>
        /// <param name="path">Path, NOT including the leading slash.</param>
        /// <returns>An HttpRequestMessage for the specified appliance path, with
        /// authorization header set using <see cref="Authenticator"/>.</returns>
        public HttpRequestMessage AuthenticatedRequest(string path)
        {
            if (this.actingAs != null)
            {
                path += (path.Contains("?") ? "&" : "?") + $"acting_as={Uri.EscapeDataString(this.actingAs)}";
            }

            return this.ApplyAuthentication(this.Request(path));
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
            string normalizedUri = Regex.Replace(uri, "(?<!:)/+", "/");

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
            if (this.disableCertCheck)
            {
                return true;
            }
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

        private HttpRequestMessage ApplyAuthentication(HttpRequestMessage request)
        {
            if (this.Authenticator == null)
            {
                throw new InvalidOperationException("Authentication required.");
            }

            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Authenticator.GetToken()));

            request.Headers.Add("Authorization", "Token token=\"" + token + "\"");
            return request;
        }
    }
}
