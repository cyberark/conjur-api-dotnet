// <copyright file="IAuthenticator.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
// Interface for authenticators.
// </summary>
namespace Conjur
{
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
    }
}
