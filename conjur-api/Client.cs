namespace Conjur
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    public class Client
    {
        private ServicePoint sp;
        private Uri applianceUri;

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

        public string Info()
        {
            this.ValidateBaseUri();
            var wr = WebRequest.CreateHttp(this.applianceUri + "info");
            return new StreamReader(wr.GetResponse().GetResponseStream()).ReadToEnd();
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

                var wr = WebRequest.CreateHttp(this.applianceUri + "info");
                wr.Method = "HEAD";
                try
                {
                    wr.GetResponse();
                }
                catch (WebException)
                {
                    // forgotten /api at the end of the Uri? Try again.
                    this.applianceUri = new Uri(this.applianceUri + "api/");
                    wr = WebRequest.CreateHttp(this.applianceUri + "info");
                    wr.Method = "HEAD";
                    wr.GetResponse();
                }

                // so it doesn't get garbage collected
                this.sp = wr.ServicePoint;
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = oldCallback;
            }
        }
    }
}
