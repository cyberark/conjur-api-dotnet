namespace Conjur
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Conjur user reference
    /// </summary>
    /// a user represents resource for a human identity
    public class User : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Conjur.User"/> class.
        /// </summary>
        /// <param name="client">Client.</param>
        /// <param name="name">Name.</param>
        internal User(Client client, string name)
            : base(client, Constants.KIND_USER, name)
        {
            // Empty Implementation
        }

        /// <summary>
        /// List of Users.
        /// </summary>
        /// <returns>The list.</returns>
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
