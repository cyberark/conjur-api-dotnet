// <copyright file="HostFactoryToken.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Host factory token.
// </summary>

namespace Conjur
{
    using System.Net;

    internal class HostFactoryToken
    {
        private readonly string token;
        private readonly Client client;

        public HostFactoryToken(Client client, string token)
        {
            this.client = client;
            this.token = token;
        }

        public Host CreateHost(string name)
        {
            var request = this.client.Request("host_factories/hosts?id="
                              + WebUtility.UrlEncode(name));
            request.Headers["Authorization"] = "Token token=\"" + this.token + "\"";
            request.Method = "POST";

            try
            {
                return JsonSerializer<Host>.Read(request);
            }
            catch (WebException e)
            {
                var hr = e.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedException("Invalid host factory token", e);
                else
                    throw;
            }
        }
    }
}