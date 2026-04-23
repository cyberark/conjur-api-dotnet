// <copyright file="AzureAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Azure Managed Identity authenticator.
// </summary>

using System.Text.Json.Serialization;

namespace Conjur;

/// <summary>
/// Azure Managed Identity authenticator.
/// Authenticates to Conjur using an Azure Managed Identity JWT token
/// obtained from the Azure Instance Metadata Service (IMDS).
/// </summary>
public class AzureAuthenticator(
    HttpClient conjurHttpClient,
    HttpClient imdsHttpClient,
    string conjurUri,
    string conjurAccount,
    string identity,
    string authenticator,
    string resourceUri = "https://management.azure.com/",
    // Fixed link-local IP reserved for Azure IMDS on all Azure VMs.
    // See: https://learn.microsoft.com/en-us/azure/virtual-machines/instance-metadata-service
    string imdsBaseUrl = "http://169.254.169.254",
    // Minimum stable API version for the IMDS managed identity token endpoint.
    // See: https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http
    string imdsApiVersion = "2018-02-01") : IAuthenticator
{
    private const string ImdsTokenPath = "/metadata/identity/oauth2/token";

    private readonly Uri conjurApiUri = new(conjurUri);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAuthenticator"/> class
    /// using default <see cref="HttpClient"/> instances.
    /// </summary>
    /// <param name="conjurUri">Base URI of the Conjur server.</param>
    /// <param name="conjurAccount">Conjur organization account name.</param>
    /// <param name="identity">Conjur host identity (login).</param>
    /// <param name="authenticator">Authenticator path, e.g. "authn-azure/my-service".</param>
    /// <param name="resourceUri">Azure resource URI for which the IMDS token is requested.</param>
    /// <param name="imdsBaseUrl">Override for the IMDS base URL. Defaults to "http://169.254.169.254".</param>
    /// <param name="imdsApiVersion">Override for the IMDS API version. Defaults to "2018-02-01".</param>
    public AzureAuthenticator(
        string conjurUri,
        string conjurAccount,
        string identity,
        string authenticator,
        string resourceUri = "https://management.azure.com/",
        string imdsBaseUrl = "http://169.254.169.254",
        string imdsApiVersion = "2018-02-01")
        : this(new HttpClient(), new HttpClient(), conjurUri, conjurAccount, identity, authenticator, resourceUri, imdsBaseUrl, imdsApiVersion)
    {
    }

    #region IAuthenticator implementation

    /// <summary>
    /// Obtain a Conjur authentication token based on Azure Managed Identity credentials.
    /// </summary>
    /// <returns>Conjur authentication token in verbatim form.
    /// It will be base64-encoded in the Client.</returns>
    public string GetToken() => GetTokenAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Obtain a Conjur authentication token based on Azure Managed Identity credentials.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Conjur authentication token in verbatim form.</returns>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var azureJwt = await GetAzureJwtAsync(cancellationToken);
        return await GetConjurTokenAsync(azureJwt, cancellationToken);
    }

    #endregion

    /// <summary>
    /// Retrieves the Conjur authenticate endpoint URL for this authenticator.
    /// </summary>
    public string GetConjurAuthenticateUrl()
    {
        var conjId = Uri.EscapeDataString(identity);

        // https://docs.conjur.org/Latest/en/Content/Developer/Conjur_API_Authenticate.htm
        // POST /{authenticator}/{account}/{login}/authenticate
        var apiUrl = conjurApiUri.ToString();
        apiUrl = apiUrl.EndsWith('/') ? apiUrl[..^1] : apiUrl;

        return $"{apiUrl}/{authenticator}/{conjurAccount}/{conjId}/authenticate";
    }

    /// <summary>
    /// Fetches the Azure Managed Identity JWT from the Azure Instance Metadata Service (IMDS).
    /// </summary>
    private async Task<string> GetAzureJwtAsync(CancellationToken cancellationToken)
    {
        var imdsUrl = $"{imdsBaseUrl}{ImdsTokenPath}" +
                      $"?api-version={imdsApiVersion}" +
                      $"&resource={Uri.EscapeDataString(resourceUri)}";

        var request = new HttpRequestMessage(HttpMethod.Get, imdsUrl);
        request.Headers.Add("Metadata", "true");

        var json = await imdsHttpClient.SendAsync(request, cancellationToken).ReadAsync(cancellationToken);

        var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<AzureImdsTokenResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize Azure IMDS token response.");

        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Azure IMDS returned an empty access token.");
        }

        return tokenResponse.AccessToken;
    }

    /// <summary>
    /// Posts the Azure JWT to Conjur and returns the decoded Conjur authentication token.
    /// The Conjur authn-azure authenticator expects the JWT as a form-encoded field named "jwt".
    /// </summary>
    private async Task<string> GetConjurTokenAsync(string azureJwt, CancellationToken cancellationToken)
    {
        var formContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("jwt", azureJwt)
        ]);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetConjurAuthenticateUrl())
        {
            Content = formContent
        };
        httpRequestMessage.Headers.AcceptEncoding.TryParseAdd("base64");

        var stringBase64Response = await conjurHttpClient.SendAsync(httpRequestMessage, cancellationToken).ReadAsync(cancellationToken);

        var data = Convert.FromBase64String(stringBase64Response);
        return Encoding.UTF8.GetString(data);
    }
}

/// <summary>
/// Represents the token response returned by the Azure Instance Metadata Service (IMDS).
/// </summary>
public class AzureImdsTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public string ExpiresIn { get; set; }

    [JsonPropertyName("resource")]
    public string Resource { get; set; }
}