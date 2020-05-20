// <copyright file="Variable.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Variable manipulation routines.
// </summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Conjur
{
    /// <summary>
    /// Conjur variable reference.
    /// </summary>
    /// Variable is a named piece of (usually secret) data stored securely on a
    /// Conjur server.
    public class Variable : Resource
    {
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Variable"/> class.
        /// </summary>
        /// <param name="client">Conjur client to use to connect.</param>
        /// <param name="name">The variable name.</param>
        /// <seealso cref="Extensions.Variable"/>
        internal Variable(Client client, string name)
            : base(client, Constants.KIND_VARIABLE, name)
        {
            this.path = $"secrets/{Uri.EscapeDataString(client.GetAccountName())}/{Constants.KIND_VARIABLE}/{Uri.EscapeDataString(name)}";
        }

        /// <summary>
        /// Gets the most recent value of the variable.
        /// </summary>
        /// <returns>The value.</returns>
        public string GetValue()
        {
            return this.Client.AuthenticatedRequest(this.path).Read();
        }

        [Obsolete ("This function is obsolete, it is recommended to use AddSecret(byte[] val) method instead")]
        public void AddSecret(string val)
        {
            AddSecret(Encoding.UTF8.GetBytes(val));
        }

        /// <summary>
        /// Set a secret (value) to this variable.
        /// </summary>
        /// <param name="val">Secret value.</param>
        public void AddSecret(byte[] val)
        {
            WebRequest webRequest = this.Client.AuthenticatedRequest(this.path);
            webRequest.Method = WebRequestMethods.Http.Post;
            if (webRequest is HttpWebRequest)
            {
                (webRequest as HttpWebRequest).AllowWriteStreamBuffering = false;
            }

            webRequest.ContentType = "text\\plain";
            webRequest.ContentLength = val.Length;
            using (Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(val, 0, val.Length);
                using (webRequest.GetResponse())
                {
                    // Intentional do not care about response content
                }
            }
         }

        internal static IEnumerable<Variable> List(Client client, string query = null, uint limit = 10000, uint offset = 0)
        {
            Func<ResourceMetadata, Variable> newInst = (searchRes) => new Variable(client, IdToName(searchRes.Id, client.GetAccountName(), Constants.KIND_VARIABLE));
            return ListResources<Variable, ResourceMetadata>(client, Constants.KIND_VARIABLE, newInst, query, limit, offset);
        }

        internal static uint Count(Client client, string query)
        {
            return CountResources(client, Constants.KIND_VARIABLE, query);
        }
    }
}
