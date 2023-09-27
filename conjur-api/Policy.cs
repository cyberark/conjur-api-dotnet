// <copyright file="Policy.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// Conjur Policy entity
// </summary>
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Conjur
{
    public class Policy : Resource
    {
        private readonly string path;

        internal Policy(Client client, string name)
                : base(client, Constants.KIND_POLICY, name)
        {
                 this.path = string.Join("/", 
                                         new string[] 
                                         {
                                            "policies",
                                            Uri.EscapeDataString(client.GetAccountName()),
                                            Constants.KIND_POLICY,
                                            Uri.EscapeDataString(name)
                                         });
        }

        /// <summary>
        /// Loading a Conjur policy MAML stream structure
        /// into given policy name, over REST request. By default POST request
        /// </summary>
        /// <param name="policyContent">Stream valid MAML Conjur policy strature.</param>
        /// <returns>Policy creation response as a stream.</returns>
        public Stream LoadPolicy(Stream policyContent, string method = WebRequestMethods.Http.Post)
        {
            var loadPolicyRequest = Client.AuthenticatedRequest(this.path);
            loadPolicyRequest.Method = new HttpMethod(method);

            policyContent.Seek(0, SeekOrigin.Begin);

            using (Stream memoryStream = new MemoryStream())
            {
                policyContent.CopyTo(memoryStream);

                using (var stream = new StreamContent(memoryStream))
                {
                    stream.Headers.ContentLength = policyContent.Length;
                    loadPolicyRequest.Content = stream;

                    var response = Client.httpClient.Send(loadPolicyRequest);
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStream();
                }
            }
        }
    }
}
