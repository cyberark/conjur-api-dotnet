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
        /// Read the response of a WebRequest.
        /// </summary>
        /// <returns>The contents of the response.</returns>
        /// <param name="request">Request to read from.</param>
        public static string Read(this WebRequest request)
        {
            return new StreamReader(request.GetResponse().GetResponseStream())
                .ReadToEnd();
        }

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
    }
}