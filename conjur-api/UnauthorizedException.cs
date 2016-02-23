// <copyright file="UnauthorizedException.cs" company="Conjur Inc.">
//     Copyright (c) 2016 Conjur Inc. All rights reserved.
// </copyright>
// <summary>
//     Unauthorized exception.
// </summary>

namespace Conjur
{
    using System;
    using System.Net;

    /// <summary>
    /// Exception raised on bad authorization.
    /// </summary>
    public class UnauthorizedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Conjur.UnauthorizedException"/> class.
        /// </summary>
        /// <param name="message">Descriptive error message.</param>
        /// <param name="exception">Wrapped exception.</param>
        public UnauthorizedException(string message, WebException exception)
            : base(message, exception)
        {
        }
    }
}
