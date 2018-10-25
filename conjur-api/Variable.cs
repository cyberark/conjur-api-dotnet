// <copyright file="Variable.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Variable manipulation routines.
// </summary>

namespace Conjur
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security;
    using System.Text;

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

        /// <summary>
        /// Gets the most recent value of the variable.
        /// </summary>
        /// <returns>The value as a SecureString.</returns>
        public SecureString GetValueAsSecureString()
        {
            return this.Client.AuthenticatedRequest(this.path).ReadAsSecureString();
        }

        /// <summary>
        /// Set a secret (value) to this variable.
        /// </summary>
        /// <param name="val">Secret value.</param>
        public void AddSecret(string val)
        {
            WebRequest webRequest = this.Client.AuthenticatedRequest(this.path);
            webRequest.Method = "POST";

            byte[] data = Encoding.UTF8.GetBytes(val);
            webRequest.ContentType = "text\\plain";
            webRequest.ContentLength = data.Length;
            webRequest.GetRequestStream().Write(data, 0, data.Length);
            using (webRequest.GetResponse())
            {
                // Intentional do not care about response content
            }
        }

        public void AddSecret(SecureString val)
        {
            WebRequest webRequest = this.Client.AuthenticatedRequest(this.path);
            webRequest.Method = "POST";
            webRequest.ContentType = "text\\plain";

            byte[] data = null;
            try 
            {
                data = val.ToByteArray();
                webRequest.ContentLength = data.Length;
                webRequest.GetRequestStream().Write(data, 0, data.Length);
                using (webRequest.GetResponse())
                {
                    // Intentional do not care about response content
                }
            }
            finally
            {
                if (data != null)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = 0;
                    }
                }
            }
        }

        internal static IEnumerable<Variable> List(Client client, string query = null)
        {
            Func<ResourceMetadata, Variable> newInst = (searchRes) => new Variable(client, IdToName(searchRes.Id, client.GetAccountName(), Constants.KIND_VARIABLE));
            return ListResources<Variable, ResourceMetadata>(client, Constants.KIND_VARIABLE, newInst, query);
        }
    }
}