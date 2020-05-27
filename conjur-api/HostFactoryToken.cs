// <copyright file="HostFactoryToken.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Host factory token.
// </summary>

using System;
using System.Net;

namespace Conjur
{
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
            WebRequest request = this.client.Request("host_factories/hosts?id="
                              + Uri.EscapeDataString(name));
            request.Headers["Authorization"] = "Token token=\"" + this.token + "\"";
            request.Method = "POST";

            try
            {
                return JsonSerializer<Host>.Read(request);
            }
            catch (WebException e)
            {
                HttpWebResponse hr = e.Response as HttpWebResponse;
                if (hr != null && hr.StatusCode == HttpStatusCode.Unauthorized) {
                    throw new UnauthorizedException("Invalid host factory token", e);
                } else {
                    throw;
                }
            }
        }
    }
}
