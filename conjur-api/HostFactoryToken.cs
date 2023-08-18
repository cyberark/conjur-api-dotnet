// <copyright file="HostFactoryToken.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Host factory token.
// </summary>

using System;
using System.Net;
using System.Net.Http;

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
            HttpRequestMessage request = this.client.Request("host_factories/hosts?id="
                              + Uri.EscapeDataString(name));
            request.Headers.Add("Authorization", "Token token=\"" + this.token + "\"");
            request.Method = HttpMethod.Post;

            try
            {
                var response = client.httpClient.Send(request);
                return JsonSerializer<Host>.Read(response);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode == HttpStatusCode.Unauthorized) {
                    throw new UnauthorizedException("Invalid host factory token", e);
                } else {
                    throw;
                }
            }
        }
    }
}
