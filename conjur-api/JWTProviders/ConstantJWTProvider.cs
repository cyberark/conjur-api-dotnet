// <copyright file="ConstantJWTProvider.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Provides a constant JWT.
// </summary>

namespace Conjur.JWTProviders;

public class ConstantJWTProvider(string value) : IJWTProvider
{
    private readonly Task<string> valueTask = Task.FromResult(value);

    public string GetJWT(object data) => value;

    public Task<string> GetJWTAsync(object data, CancellationToken cancellationToken = default) => valueTask;
}
