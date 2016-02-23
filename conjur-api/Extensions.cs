// <copyright file="Extensions.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Utility extension methods.
// </summary>

namespace Conjur
{
    using System.IO;
    using System.Net;

    /// <summary>
    /// Utility extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Creates an object representing the named variable.
        /// </summary>
        /// Note the existence of the variable is not verified.
        /// <param name="client">Conjur client to use to connect.</param>
        /// <param name="name">The variable name.</param>
        /// <returns>Variable object.</returns>
        /// <seealso cref="Variable()"/>
        public static Variable Variable(this Client client, string name)
        {
            return new Variable(client, name);
        }

        /// <summary>
        /// Creates a host using a host factory token.
        /// </summary>
        /// <returns>The created host.</returns>
        /// <param name="client">Conjur client.</param>
        /// <param name="name">Name of the host to create.</param>
        /// <param name="hostFactoryToken">Host factory token.</param>
        public static Host CreateHost(
            this Client client, 
            string name, 
            string hostFactoryToken)
        {
            return new HostFactoryToken(client, hostFactoryToken)
                .CreateHost(name);
        }

        /// <summary>
        /// Creates an object representing a Conjur resource.
        /// </summary>
        /// <param name="client">Conjur client instance.</param>
        /// <param name="kind">Resource kind.</param>
        /// <param name="id">Resource identifier.</param>
        /// <returns>Object representing the specified resource.</returns>
        public static Resource Resource(this Client client, string kind, string id)
        {
            return new Resource(client, kind, id);
        }

        /// <summary>
        /// Read the response of a WebRequest.
        /// </summary>
        /// <returns>The contents of the response.</returns>
        /// <param name="request">Request to read from.</param>
        internal static string Read(this WebRequest request)
        {
            return new StreamReader(request.GetResponse().GetResponseStream())
                .ReadToEnd();
        }
    }
}