// <copyright file="Policy.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// Conjur Policy entity
// </summary>

namespace Conjur;

public class Policy : Resource
{
    private readonly string path;

    internal Policy(Client client, string name)
        : base(client, Constants.KIND_POLICY, name)
    {
        path = string.Join("/", "policies", Uri.EscapeDataString(client.AccountName), Constants.KIND_POLICY, Uri.EscapeDataString(name));
    }

    /// <summary>
    /// Loading a Conjur policy MAML stream structure
    /// into given policy name, over REST request. By default, POST request
    /// </summary>
    /// <param name="policyContent">Stream with valid MAML Conjur policy structure.</param>
    /// <param name="method"></param>
    /// <returns>Policy creation response as a stream.</returns>
    public Stream LoadPolicy(Stream policyContent, string method = WebRequestMethods.Http.Post)
    {
        var loadPolicyRequest = Client.AuthenticatedRequest(path);
        loadPolicyRequest.Method = new HttpMethod(method);

        policyContent.Seek(0, SeekOrigin.Begin);

        using var memoryStream = new MemoryStream();
        policyContent.CopyTo(memoryStream);

        using var stream = new StreamContent(memoryStream);
        stream.Headers.ContentLength = policyContent.Length;
        loadPolicyRequest.Content = stream;

        var response = Client.Send(loadPolicyRequest);
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStream();
    }

    /// <summary>
    /// Loading a Conjur policy MAML stream structure
    /// into given policy name, over REST request. By default, POST request
    /// </summary>
    /// <param name="policyContent">Stream with valid MAML Conjur policy structure.</param>
    /// <param name="method"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Policy creation response as a stream.</returns>
    public async Task<Stream> LoadPolicyAsync(Stream policyContent, string method = WebRequestMethods.Http.Post, CancellationToken cancellationToken = default)
    {
        var loadPolicyRequest = await Client.AuthenticatedRequestAsync(path, cancellationToken);
        loadPolicyRequest.Method = new HttpMethod(method);

        policyContent.Seek(0, SeekOrigin.Begin);

        await using var memoryStream = new MemoryStream();
        await policyContent.CopyToAsync(memoryStream, cancellationToken);

        using var stream = new StreamContent(memoryStream);
        stream.Headers.ContentLength = policyContent.Length;
        loadPolicyRequest.Content = stream;

        var response = await Client.SendAsync(loadPolicyRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
