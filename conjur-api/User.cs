// <copyright file="User.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     User manipulation routines.
// </summary>

namespace Conjur;

/// <summary>
/// A user represents resource for a human identity.
/// </summary>
public class User : Resource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Conjur.User"/> class.
    /// </summary>
    /// <param name="client">API client.</param>
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
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <returns>Returns IEnumerable to User.</returns>
    internal static IEnumerable<User> List(Client client, string query = null, uint limit = 10000, uint offset = 0)
        => ListResources<User, ResourceMetadata>(client, Constants.KIND_USER, r => Map(client, r), query, limit, offset);

    /// <summary>
    /// List of Users.
    /// </summary>
    /// <param name="client">Conjur Client to query.</param>
    /// <param name="query">Query to search.</param>
    /// <param name="limit"></param>
    /// <param name="offset"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Returns IEnumerable to User.</returns>
    internal static IAsyncEnumerable<User> ListAsync(Client client, string query = null, uint limit = 10000, uint offset = 0, CancellationToken cancellationToken = default)
        => ListResourcesAsync<User, ResourceMetadata>(client, Constants.KIND_USER, r => Map(client, r), query, limit, offset, cancellationToken);

    private static User Map(Client client, ResourceMetadata searchRes) => new(client, IdToName(searchRes.Id, client.AccountName, Constants.KIND_USER));
}
