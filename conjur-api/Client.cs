// <copyright file="Client.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Base Conjur client class implementation.
// </summary>
namespace Conjur
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Conjur API client.
    /// </summary>
    public class Client
    {
        private ServicePoint sp;
        private Uri applianceUri;
        private string account;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Client"/> class.
        /// </summary>
        /// <param name="applianceUri">Appliance URI.</param>
        public Client(string applianceUri)
        {
            this.applianceUri = NormalizeBaseUri(applianceUri);
        }

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
        /// Logs in using a password.
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="userName">User name to log in as (for example "bob"
        /// or "host/example.com".</param>
        /// <param name="password">Password of the user.</param>
        public string LogIn(string userName, string password)
        {
            this.ValidateBaseUri();
            var wr = this.Request("authn/users/login");
            wr.PreAuthenticate = true;
            wr.Credentials = new NetworkCredential(userName, password);
            var apiKey = Read(wr);

            // TODO: actually do something with the api key
            return apiKey;
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
        private static bool ValidateCertificate(
            object sender, 
            X509Certificate certificate, 
            X509Chain chain, 
            SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    // TODO: do real validation
                case SslPolicyErrors.None:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Read the response of a WebRequest.
        /// </summary>
        /// <returns>The contents of the response.</returns>
        /// <param name="request">Request to read from.</param>
        private static string Read(WebRequest request)
        {
            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }

        /// <summary>
        /// Create a WebRequest for the specified path.
        /// </summary>
        /// <param name="path">Path, NOT including the leading slash.</param>
        /// <returns>A WebRequest for the specified appliance path.</returns>
        private WebRequest Request(string path)
        {
            return WebRequest.Create(this.applianceUri + path);
        }

        /// <summary>
        /// Get the server info.
        /// </summary>
        /// <returns>Server information.</returns>
        private ServerInfo Info()
        {
            this.ValidateBaseUri();
            var wr = this.Request("info");
            var serializer = new DataContractJsonSerializer(typeof(ServerInfo));
            return (ServerInfo)serializer.ReadObject(wr.GetResponse().GetResponseStream());
        }

        /// <summary>
        /// Validates the appliance base URI.
        /// Tries to connect to /info; if not successful, try again adding an /api prefix.
        /// Also sets up certificate validation.
        /// </summary>
        private void ValidateBaseUri()
        {
            /*
                HACK: This dance is to make sure the validation callback only applies to our server.
                There's no way to set per-request validation in .NET < 4.5, but it is my
                understanding that ServicePoints (which are per-server) store the validators
                for later usage. Thus we set the default validator, make a request to
                instantiate a ServicePoint, then stash it in an instance variable so
                that it doesn't get garbage collected. Finally we restore the original.
            */

            if (this.sp != null)
            {
                return;
            }
                
            var oldCallback = ServicePointManager.ServerCertificateValidationCallback;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = 
                    new RemoteCertificateValidationCallback(ValidateCertificate);

                var wr = this.Request("info");
                wr.Method = "HEAD";
                try
                {
                    wr.GetResponse();
                }
                catch (WebException)
                {
                    // forgotten /api at the end of the Uri? Try again.
                    this.applianceUri = new Uri(this.applianceUri + "api/");
                    wr = this.Request("info");
                    wr.Method = "HEAD";
                    wr.GetResponse();
                }

                // so it doesn't get garbage collected
                HttpWebRequest hwr = wr as HttpWebRequest;
                if (hwr != null)
                    this.sp = hwr.ServicePoint;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = oldCallback;
            }
        }

        [DataContract]
        internal class ServerInfo
        {
            [DataMember]
            internal string account;
        }
    }
}
