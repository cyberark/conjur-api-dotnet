// <copyright file="Variable.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Variable manipulation routines.
// </summary>

namespace Conjur
{
    using System;
    using System.Json;

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
            this.path = "variables/" + Uri.EscapeDataString(name);
        }

        /// <summary>
        /// Gets the most recent value of the variable.
        /// </summary>
        /// <returns>The value.</returns>
        public string GetValue()
        {
            return this.Client.AuthenticatedRequest(this.path + "/value").Read();
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
    }
}
