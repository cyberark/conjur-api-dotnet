// <copyright file="User.cs" company="Conjur Inc.">
//     Copyright (c) 2016-2018 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     User manipulation routines.
// </summary>

namespace Conjur
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Conjur User reference.
    /// </summary>
    /// A user represents an identity for a human. It is a role, in RBAC terms.
    public class User : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.User"/> class.
        /// </summary>
        /// <param name="client">Conjur client to use to connect.</param>
        /// <param name="name">The User (or host) name.</param>
        internal User(Client client, string name)
            : base(client, "user", name)
        {
        }

        /// <summary>
        /// Search/List for Users
        /// </summary>
        /// <param name="client">Conjur client to query.</param>
        /// <param name="query">Query for search.</param>
        /// <returns>Returns IEnumerable to User.</returns>
        internal static IEnumerable<User> List(Client client, string query = null)
        {
            Func<ResourceMetadata, User> newInst = (searchRes) => new User(client, searchRes.Id);
            return ListResources<User, ResourceMetadata>(client, "user", newInst, query);
        }
    }
}
