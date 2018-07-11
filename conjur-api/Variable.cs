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
            this.path = $"secrets/{WebUtility.UrlEncode(client.GetAccountName())}/{Constants.KIND_VARIABLE}/{WebUtility.UrlEncode(name)}";
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
        }

        internal static IEnumerable<Variable> List(Client client, string query = null)
        {
            Func<ResourceMetadata, Variable> newInst = (searchRes) => new Variable(client, IdToName(searchRes.Id, client.GetAccountName(), Constants.KIND_VARIABLE));
            return ListResources<Variable, ResourceMetadata>(client, Constants.KIND_VARIABLE, newInst, query);
        }
    }
}
