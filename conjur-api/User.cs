// <copyright file="User.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     User manipulation routines.
// </summary>
namespace Conjur
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A user represents resource for a human identity.
    /// </summary>
    public class User : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Conjur.User"/> class.
        /// </summary>
        /// <param name="client">Active API client.</param>
        /// <param name="name">A name of requested user.</param>
        internal User(Client client, string name)
            : base(client, Constants.KIND_USER, name)
        {
            // Empty Implementation.
        }

        /// <summary>
        /// List of Users.
        /// </summary>
        /// <param name="client">Conjur Client to query.</param>
        /// <param name="query">Query to search.</param>
        /// <returns>Returns IEnumerable to User.</returns>
        internal static IEnumerable<User> List(Client client, string query = null)
        {
            Func<ResourceMetadata, User> newInst = (searchRes) => new User(client, IdToName(searchRes.Id, client.GetAccountName(), Constants.KIND_USER));
            return ListResources<User, ResourceMetadata>(client, Constants.KIND_USER, newInst, query);
        }
    }
}