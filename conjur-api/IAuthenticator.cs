// <copyright file="IAuthenticator.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
// Interface for authenticators. 
// Authenticators apply authentication to a web request.
// </summary>
namespace Conjur
{
    using System.Net;

    /// <summary>
    /// Interface for authenticators. 
    /// Authenticators apply authentication to a web request.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Apply the authentication to a WebRequest.
        /// </summary>
        /// <param name="webRequest">Web request to apply the authentication to.</param>
        void Apply(HttpWebRequest webRequest);
    }
}
