// <copyright file="JsonSerializer.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// JSON utilities.
// </summary>

using System.Runtime.Serialization.Json;

namespace Conjur;

internal static class JsonSerializer<T> where T : class
{
    // Analysis disable once StaticFieldInGenericType
    // (The behaviour is exactly what I want: one instance per T.)
    private static readonly DataContractJsonSerializer Instance = new(typeof(T));

    public static T Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream();
        return Instance.ReadObject(stream) as T;
    }

    public static async Task<T> ReadAsync(Client client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return Instance.ReadObject(stream) as T;
    }
}
