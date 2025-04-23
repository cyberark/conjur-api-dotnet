// <copyright file="Resource.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Base class representing a Conjur resource.
// </summary>

using System.Runtime.CompilerServices;

namespace Conjur;

/// <summary>
/// Base class representing a Conjur resource.
/// </summary>
public class Resource
{
    /// <summary>
    /// The Conjur client used to manipulate this resource.
    /// </summary>
    protected readonly Client Client;

    /// <summary>
    /// Gets resource name.
    /// </summary>
    /// <value>The name of the resource.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the resource identifier, in format of account:kind:name.
    /// </summary>
    /// <value>The identifier.</value>
    public string Id { get; }

    /// <summary>
    /// Gets the resource Kind.
    /// </summary>
    /// <value>The kind.</value>
    public string Kind { get; }

    /// <summary>
    /// Gets the resource path.
    /// </summary>
    /// <value>The resource path.</value>
    protected string ResourcePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Conjur.Resource"/> class.
    /// </summary>
    /// <param name="client">Conjur client used to manipulate this resource.</param>
    /// <param name="kind">Resource kind.</param>
    /// <param name="name">Resource name.</param>
    internal Resource(Client client, string kind, string name)
    {
        Client = client;
        Kind = kind;
        Name = name;
        Id = $"{client.AccountName}:{kind}:{Name}";
        ResourcePath = $"resources/{Uri.EscapeDataString(Client.AccountName)}/{Uri.EscapeDataString(Kind)}/{Uri.EscapeDataString(Name)}";
    }

    /// <summary>
    /// Determines whether the authenticated user holds the specified privilege
    /// on this resource.
    /// </summary>
    /// <returns><c>true</c> if the authenticated user holds the specified
    /// privilege; otherwise, <c>false</c>.</returns>
    /// <param name="privilege">Privilege to check.</param>
    public bool Check(string privilege)
    {
        var req = Client.AuthenticatedRequest($"{ResourcePath}/?check=true&privilege={Uri.EscapeDataString(privilege)}");
        req.Method = HttpMethod.Head;

        try
        {
            var response = Client.Send(req);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException exn) when (exn.StatusCode == HttpStatusCode.Forbidden)
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether the authenticated user holds the specified privilege
    /// on this resource.
    /// </summary>
    /// <returns><c>true</c> if the authenticated user holds the specified
    /// privilege; otherwise, <c>false</c>.</returns>
    /// <param name="privilege">Privilege to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public async Task<bool> CheckAsync(string privilege, CancellationToken cancellationToken = default)
    {
        var req = await Client.AuthenticatedRequestAsync($"{ResourcePath}/?check=true&privilege={Uri.EscapeDataString(privilege)}", cancellationToken);
        req.Method = HttpMethod.Head;

        try
        {
            var response = await Client.SendAsync(req, cancellationToken);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException exn) when (exn.StatusCode == HttpStatusCode.Forbidden)
        {
            return false;
        }
    }

    internal static IEnumerable<T> ListResources<T, TResult>(Client client, string kind, Func<TResult, T> map, string query = null, uint limit = 10000, uint offset = 0)
    {
        List<TResult> resultList;
        do
        {
            var pathListResourceQuery = $"resources/{client.AccountName}/{kind}?offset={offset}&limit={limit}"
                                        + ((query is not null) ? $"&search={query}" : string.Empty);

            resultList = JsonSerializer<List<TResult>>.Read(client.Send(client.AuthenticatedRequest(pathListResourceQuery)));
            foreach (var searchResult in resultList)
            {
                yield return map(searchResult);
            }

            offset += (uint)resultList.Count;
        } while (resultList.Count > 0 && offset % limit == 0);
    }

    internal static async IAsyncEnumerable<T> ListResourcesAsync<T, TResult>(Client client, string kind, Func<TResult, T> map, string query = null, uint limit = 10000, uint offset = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<TResult> resultList;
        do
        {
            var pathListResourceQuery = $"resources/{client.AccountName}/{kind}?offset={offset}&limit={limit}"
                                        + (query is not null ? $"&search={query}" : string.Empty);

            var request = await client.AuthenticatedRequestAsync(pathListResourceQuery, cancellationToken);
            resultList = await JsonSerializer<List<TResult>>.ReadAsync(client, request, cancellationToken);
            foreach (var searchResult in resultList)
            {
                yield return map(searchResult);
            }

            offset += (uint)resultList.Count;
        } while (resultList.Count > 0 && offset % limit == 0);
    }

    internal static uint CountResources(Client client, string kind, string query = null)
    {
        var pathCountResourceQuery = $"resources/{client.AccountName}/{kind}?count=true" + ((query != null) ? $"&search={query}" : string.Empty);
        var countJsonObj = JsonSerializer<CountResult>.Read(client.Send(client.AuthenticatedRequest(pathCountResourceQuery)));
        return Convert.ToUInt32(countJsonObj.Count);
    }

    internal static async Task<uint> CountResourcesAsync(Client client, string kind, string query = null, CancellationToken cancellationToken = default)
    {
        var pathCountResourceQuery = $"resources/{client.AccountName}/{kind}?count=true" + ((query != null) ? $"&search={query}" : string.Empty);
        var request = await client.AuthenticatedRequestAsync(pathCountResourceQuery, cancellationToken);
        var countJsonObj = await JsonSerializer<CountResult>.ReadAsync(client, request, cancellationToken);
        return countJsonObj.Count;
    }

    /// <summary>
    /// Parse Conjur id following format of account:kind:name to extract name.
    /// </summary>
    /// <returns>Extracted name from id.</returns>
    /// <param name="id">Conjur Identifier.</param>
    /// <param name="account">Conjur Account.</param>
    /// <param name="kind">Conjur resource kind.</param>
    protected internal static string IdToName(string id, string account, string kind) => id[(account.Length + kind.Length + 2)..];
}
