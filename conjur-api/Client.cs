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

    public class Client
    {
        private ServicePoint sp;
        private Uri applianceUri;
        private string account;

        public Client(string applianceUri)
        {
            this.applianceUri = NormalizeBaseUri(applianceUri);
        }

        public Uri ApplianceUri
        {
            get
            { 
                return this.applianceUri;
            }
        }

        public string GetAccountName()
        {
            if (this.account == null)
                this.account = Info().account;
            return this.account;
        }

        /// <summary>
        /// Logs in using a password.
        /// </summary>
        /// <returns>The API key.</returns>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        public string LogIn(string userName, string password)
        {
            this.ValidateBaseUri();
            var wr = Request("authn/users/login");
            wr.PreAuthenticate = true;
            wr.Credentials = new NetworkCredential(userName, password);
            var apiKey = Read(wr);
            // TODO: actually do something with the api key
            return apiKey;
        }

        private WebRequest Request(string path)
        {
            return WebRequest.Create(this.applianceUri + path);
        }

        static private string Read(WebRequest request)
        {
            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }

        private ServerInfo Info()
        {
            this.ValidateBaseUri();
            var wr = Request("info");
            var serializer = new DataContractJsonSerializer(typeof(ServerInfo));
            return (ServerInfo)serializer.ReadObject(wr.GetResponse().GetResponseStream());
        }

        private static Uri NormalizeBaseUri(string uri)
        {
            // appliance's nginx doesn't like double slashes,
            // eg. it returns 401 on https://example.org//api/info

            // so normalize to remove double slashes
            var normalizedUri = Regex.Replace(uri, "(?<!:)/+", "/");

            // make sure there is a trailing slash
            return new Uri(Regex.Replace(normalizedUri, "(?<!/)\\z", "/"));
        }

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

                var wr = Request("info");
                wr.Method = "HEAD";
                try
                {
                    wr.GetResponse();
                }
                catch (WebException)
                {
                    // forgotten /api at the end of the Uri? Try again.
                    this.applianceUri = new Uri(this.applianceUri + "api/");
                    wr = Request("info");
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
