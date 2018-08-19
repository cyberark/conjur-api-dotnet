// <copyright file="Policy.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
// Conjur Policy entity
// </summary>
namespace Conjur
{
    using System.IO;
    using System.Net;

    public class Policy : Resource
    {
        private readonly string path;

        internal Policy(Client client, string name)
                : base(client, Constants.KIND_POLICY, name)
        {
            this.path = $"policies/{WebUtility.UrlEncode(client.GetAccountName())}/{Constants.KIND_POLICY}/{WebUtility.UrlEncode(name)}";
        }

        /// <summary>
        /// Loading a Conjur policy MAML stream structure
        /// into given policy name, over REST POST request.
        /// </summary>
        /// <param name="policyContent">Stream valid MAML Conjur policy strature.</param>
        /// <returns>Policy creation response as a stream</returns>
        public Stream LoadPolicy(Stream policyContent)
        {
            WebRequest req = Client.AuthenticatedRequest(this.path);
            req.Method = WebRequestMethods.Http.Post;
            req.ContentLength = policyContent.Length;
            policyContent.Seek(0, SeekOrigin.Begin);

            using (Stream reqStream = req.GetRequestStream())
            {
                policyContent.CopyTo(reqStream);
                using (Stream resStream = req.GetResponse().GetResponseStream())
                {
                    return resStream;
                }
            }
        }
    }
}