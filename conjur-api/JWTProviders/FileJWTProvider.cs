// <copyright file="FileJWTProvider.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Provides a JWT from a file, file is always read, not cached.
// </summary>

namespace Conjur.JWTProviders;

public class FileJWTProvider(string filePath = Constants.K8S_JWT_PATH) : IJWTProvider
{
    public string GetJWT(object data) => File.ReadAllText(filePath);

    public async Task<string> GetJWTAsync(object data, CancellationToken cancellationToken = default)
        => await File.ReadAllTextAsync(filePath, cancellationToken);
}
