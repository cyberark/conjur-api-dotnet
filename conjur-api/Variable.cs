// <copyright file="Variable.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Variable manipulation routines.
// </summary>

using System.Net.Http.Headers;

namespace Conjur;

/// <summary>
/// Conjur variable reference.
/// </summary>
/// Variable is a named piece of (usually secret) data stored securely on a
/// Conjur server.
public class Variable : Resource
{
    private readonly string path;

    /// <summary>
    /// Initializes a new instance of the <see cref="Variable"/> class.
    /// </summary>
    /// <param name="client">Conjur client to use to connect.</param>
    /// <param name="name">The variable name.</param>
    /// <seealso cref="Client.Variable"/>
    internal Variable(Client client, string name)
        : base(client, Constants.KIND_VARIABLE, name)
    {
        path = $"secrets/{Uri.EscapeDataString(client.AccountName)}/{Constants.KIND_VARIABLE}/{Uri.EscapeDataString(name)}";
    }

    /// <summary>
    /// Gets the most recent value of the variable.
    /// </summary>
    /// <returns>The value.</returns>
    public string GetValue()
    {
        var request = Client.AuthenticatedRequest(path);
        return Client.Send(request).Read();
    }

    /// <summary>
    /// Gets the most recent value of the variable.
    /// </summary>
    /// <returns>The value.</returns>
    public async Task<string> GetValueAsync(CancellationToken cancellationToken = default)
    {
        var request = await Client.AuthenticatedRequestAsync(path, cancellationToken);
        return await Client.SendAsync(request, cancellationToken).ReadAsync(cancellationToken);
    }

    [Obsolete("This function is obsolete, it is recommended to use AddSecret(byte[] val) method instead")]
    public void AddSecret(string val) => AddSecret(Encoding.UTF8.GetBytes(val));

    /// <summary>
    /// Set a secret (value) to this variable.
    /// </summary>
    /// <param name="val">Secret value.</param>
    public void AddSecret(byte[] val)
    {
        var request = Client.AuthenticatedRequest(path);
        request.Method = HttpMethod.Post;

        using var content = new ByteArrayContent(val);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        request.Content = content;

        var response = Client.Send(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Set a secret (value) to this variable.
    /// </summary>
    /// <param name="val">Secret value.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public async Task AddSecretAsync(byte[] val, CancellationToken cancellationToken = default)
    {
        var request = await Client.AuthenticatedRequestAsync(path, cancellationToken);
        request.Method = HttpMethod.Post;

        using var content = new ByteArrayContent(val);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        request.Content = content;

        var response = await Client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    internal static IEnumerable<Variable> List(Client client, string query = null, uint limit = 10000, uint offset = 0)
        => ListResources<Variable, ResourceMetadata>(client, Constants.KIND_VARIABLE, s => Map(client, s), query, limit, offset);

    internal static IAsyncEnumerable<Variable> ListAsync(Client client, string query = null, uint limit = 10000, uint offset = 0, CancellationToken cancellationToken = default)
        => ListResourcesAsync<Variable, ResourceMetadata>(client, Constants.KIND_VARIABLE, s => Map(client, s), query, limit, offset, cancellationToken);

    internal static uint Count(Client client, string query)
        => CountResources(client, Constants.KIND_VARIABLE, query);

    internal static Task<uint> CountAsync(Client client, string query, CancellationToken cancellationToken)
        => CountResourcesAsync(client, Constants.KIND_VARIABLE, query, cancellationToken);

    private static Variable Map(Client client, ResourceMetadata searchRes)
        => new(client, IdToName(searchRes.Id, client.AccountName, Constants.KIND_VARIABLE));
}
