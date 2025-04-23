// <copyright file="IAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
// Interface for authenticators.
// </summary>

namespace Conjur;

/// <summary>
/// Interface for authenticators, which are used to generate Conjur
/// authentication tokens.
/// </summary>
public interface IAuthenticator
{
    /// <summary>
    /// Obtain a Conjur authentication token.
    /// </summary>
    /// <returns>Conjur authentication token in verbatim form.
    /// It needs to be base64-encoded to be used in a web request.</returns>
    string GetToken();

    /// <summary>
    /// Obtain a Conjur authentication token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Conjur authentication token in verbatim form.
    /// It needs to be base64-encoded to be used in a web request.</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
