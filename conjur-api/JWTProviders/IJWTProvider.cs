// <copyright file="IJWTProvider.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Interface for JWT provider.
// </summary>

namespace Conjur.JWTProviders;

/// <summary>
/// Interface for JWT providers, which are used to retrieve JWT token
/// for JWT Conjur authentication.
/// </summary>
public interface IJWTProvider
{
    string GetJWT(object data) => GetJWTAsync(data, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    Task<string> GetJWTAsync(object data, CancellationToken cancellationToken = default);
}