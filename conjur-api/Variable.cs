// <copyright file="Variable.cs" company="Conjur Inc.">
//     Copyright (c) 2016-2018 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Variable manipulation routines.
// </summary>

namespace Conjur
{
    using System;
    using System.Collections.Generic;
    using System.Json;    //using System.Json;

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
            : base(client, "variable", name)
        {
            this.path = client.GetAccountName() + "/variable/" + Uri.EscapeDataString(name);
        }

        /// <summary>
        /// Gets the most recent value of the variable.
        /// </summary>
        /// <returns>The value.</returns>
        public string GetValue()
        {
            var req = this.Client.AuthenticatedRequest("secrets/" + this.path);
            req.Method = "GET";
            return req.Read();
        }

        /// <summary>
        /// Adds a value to the variable.
        /// </summary>
        /// <param name="value">Value to be added to the Variable.</param>
        public void AddValue(string value)
        {
            var req = this.Client.AuthenticatedRequest($"{this.path}/values");
            req.Method = "POST";
            req.ContentType = "application/json";

            using (var dataStream = req.GetRequestStream()) {
                JsonObject jsonObject = new JsonObject();
                jsonObject.Add("value", value);
                using (var dataStreamWriter = new System.IO.StreamWriter(dataStream)) {
                    dataStreamWriter.Write(jsonObject.ToString());
                }
            }

            req.GetResponse().Close();
        }

        /// <summary>
        /// Search for variables
        /// </summary>
        /// <param name="client">Conjur client to query.</param>
        /// <param name="query">Query for search.</param>
        /// <returns>Returns IEnumerable to Variable.</returns>
        internal static IEnumerable<Variable> List(Client client, string query = null)
        {
            Func<ResourceMetadata, Variable> newInst = (searchRes) => new Variable(client, searchRes.Id);
            return ListResources<Variable, ResourceMetadata>(client, "variable", newInst, query);
        }
    }
}
