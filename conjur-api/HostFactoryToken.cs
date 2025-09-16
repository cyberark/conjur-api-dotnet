// <copyright file="HostFactoryToken.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Host factory token.
// </summary>

namespace Conjur;

internal class HostFactoryToken(Client client, string token)
{
    public Host CreateHost(string name)
    {
        var request = BuildCreateHostRequest(name);

        try
        {
            var response = client.Send(request);
            return JsonSerializer<Host>.Read(response);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException("Invalid host factory token", e);
        }
    }

    public async Task<Host> CreateHostAsync(string name, CancellationToken cancellationToken)
    {
        var request = BuildCreateHostRequest(name);

        try
        {
            return await JsonSerializer<Host>.ReadAsync(client, request, cancellationToken);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException("Invalid host factory token", e);
        }
    }

    private HttpRequestMessage BuildCreateHostRequest(string name)
    {
        var request = client.Request("host_factories/hosts?id=" + Uri.EscapeDataString(name));
        request.Headers.Add("Authorization", $"Token token=\"{token}\"");
        request.Method = HttpMethod.Post;
        return request;
    }
}
