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
    using System.Net.Mime;
    using System.Text;

    /// <summary>
    /// Conjur variable reference.
    /// </summary>
    /// Variable is a named piece of (usually secret) data stored securely on a
    /// Conjur server.
    public class Variable : Resource
    {
        private readonly string m_path;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.Variable"/> class.
        /// </summary>
        /// <param name="client">Conjur client to use to connect.</param>
        /// <param name="name">The variable name.</param>
        /// <seealso cref="Extensions.Variable"/>
        internal Variable(Client client, string name)
            : base(client, ResourceKind.variable, name)
        {
            m_path = $"secrets/{WebUtility.UrlEncode(client.GetAccountName())}/{ResourceKind.variable}/{WebUtility.UrlEncode(name)}";
        }

        /// <summary>
        /// Gets the most recent value of the variable.
        /// </summary>
        /// <returns>The value.</returns>
        public string GetValue()
        {
            return m_client.AuthenticatedRequest(m_path).Read();
        }

        public void AddSecret(string val)
        {
            byte[] value = Encoding.UTF8.GetBytes(val);
            WebRequest webRequest = m_client.AuthenticatedRequest(m_path);
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = "text\\plain";
            webRequest.ContentLength = value.Length;
            webRequest.GetRequestStream().Write(value, 0, value.Length);
        }

        internal static IEnumerable<Variable> List(Client client, string query = null)
        {
            Func<ResourceMetadata, Variable> newInst = (searchRes) => new Variable(client, IdToName(searchRes.Id, client.GetAccountName(), ResourceKind.variable));
            return ListResources<Variable, ResourceMetadata>(client, ResourceKind.variable, newInst, query);
        }
    }
}
