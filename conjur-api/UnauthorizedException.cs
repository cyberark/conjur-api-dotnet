// <copyright file="UnauthorizedException.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2020 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Unauthorized exception.
// </summary>

using System;
using System.Net;

namespace Conjur
{
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
