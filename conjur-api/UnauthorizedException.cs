// <copyright file="UnauthorizedException.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Unauthorized exception.
// </summary>

namespace Conjur;

/// <summary>
/// Exception raised on bad authorization.
/// </summary>
public class UnauthorizedException(string message, HttpRequestException exception) : Exception(message, exception);
