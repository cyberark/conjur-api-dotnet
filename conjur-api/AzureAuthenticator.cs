// <copyright file="AzureAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Azure Managed Identity authenticator.
// </summary>

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Conjur;

/// <summary>
/// Azure Managed Identity authenticator.
/// Authenticates to Conjur using an Azure Managed Identity JWT token
/// obtained from the Azure Instance Metadata Service (IMDS).
/// </summary>
public class AzureAuthenticator : IAuthenticator
{
    private const string ImdsTokenPath = "/metadata/identity/oauth2/token";

    private readonly HttpClient conjurHttpClient;
    private readonly HttpClient imdsHttpClient;
    private readonly Uri conjurApiUri;
    private readonly string conjurAccount;
    private readonly string identity;
    private readonly string authenticator;
    private readonly string resourceUri;
    private readonly string imdsBaseUrl;
    private readonly string imdsApiVersion;

    private bool disableCertCheck;

    /// <summary>
    /// Gets the collection of extra certificates trusted for authenticating the
    /// Conjur server, in addition to the system ones.
    /// Useful when Conjur is configured with a self-signed or internal CA certificate.
    /// </summary>
    public X509Certificate2Collection TrustedCertificates { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAuthenticator"/> class
    /// with injected <see cref="HttpClient"/> instances. Intended for testing.
    /// </summary>
    /// <param name="conjurHttpClient">HTTP client used to call the Conjur authenticate endpoint.</param>
    /// <param name="imdsHttpClient">HTTP client used to call the Azure IMDS endpoint.</param>
    /// <param name="conjurUri">Base URI of the Conjur server.</param>
    /// <param name="conjurAccount">Conjur organization account name.</param>
    /// <param name="identity">Conjur host identity (login).</param>
    /// <param name="authenticator">Authenticator path, e.g. "authn-azure/my-service".</param>
    /// <param name="resourceUri">Azure resource URI for which the IMDS token is requested.</param>
    /// <param name="imdsBaseUrl">Override for the IMDS base URL. Defaults to "http://169.254.169.254".</param>
    /// <param name="imdsApiVersion">Override for the IMDS API version. Defaults to "2018-02-01".</param>
    public AzureAuthenticator(
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
        string imdsApiVersion = "2018-02-01")
    {
        this.conjurHttpClient = conjurHttpClient;
        this.imdsHttpClient = imdsHttpClient;
        this.conjurApiUri = new Uri(conjurUri);
        this.conjurAccount = conjurAccount;
        this.identity = identity;
        this.authenticator = authenticator;
        this.resourceUri = resourceUri;
        this.imdsBaseUrl = imdsBaseUrl;
        this.imdsApiVersion = imdsApiVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAuthenticator"/> class
    /// with production-ready <see cref="HttpClient"/> instances.
    /// The IMDS client bypasses the system proxy (required on Windows — 169.254.169.254
    /// is a link-local address and must never be routed through a proxy).
    /// The Conjur client uses certificate validation that supports custom trusted certificates.
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
        // Fixed link-local IP reserved for Azure IMDS on all Azure VMs.
        // See: https://learn.microsoft.com/en-us/azure/virtual-machines/instance-metadata-service
        string imdsBaseUrl = "http://169.254.169.254",
        // Minimum stable API version for the IMDS managed identity token endpoint.
        // See: https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token#get-a-token-using-http
        string imdsApiVersion = "2018-02-01")
    {
        this.conjurApiUri = new Uri(conjurUri);
        this.conjurAccount = conjurAccount;
        this.identity = identity;
        this.authenticator = authenticator;
        this.resourceUri = resourceUri;
        this.imdsBaseUrl = imdsBaseUrl;
        this.imdsApiVersion = imdsApiVersion;

        // IMDS runs on a link-local address (169.254.169.254) that must never be
        // routed through a system proxy. On Windows, HttpClient uses the system
        // proxy by default, which would cause IMDS requests to fail.
        this.imdsHttpClient = new HttpClient(new HttpClientHandler
        {
            UseProxy = false
        });

        // Conjur client with certificate validation. Supports extra trusted roots
        // via TrustedCertificates (e.g. when Conjur uses a self-signed or internal CA cert).
        this.conjurHttpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidateCertificate
        });
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
    /// Disables SSL certificate validation for the Conjur connection.
    /// Warning: this is a security risk and should only be used for testing purposes.
    /// </summary>
    public void DisableCertCheck() => disableCertCheck = true;

    /// <summary>
    /// Re-enables SSL certificate validation for the Conjur connection.
    /// This is already the default; only needed if <see cref="DisableCertCheck"/> was called.
    /// </summary>
    public void EnableCertCheck() => disableCertCheck = false;

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
    /// Validates the Conjur server certificate.
    /// Supports extra trusted roots supplied via <see cref="TrustedCertificates"/>.
    /// </summary>
    private bool ValidateCertificate(
        HttpRequestMessage request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (disableCertCheck)
            return true;

        return sslPolicyErrors switch
        {
            SslPolicyErrors.None => true,
            SslPolicyErrors.RemoteCertificateChainErrors => chain.VerifyWithExtraRoots(certificate, TrustedCertificates),
            _ => false
        };
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
            throw new InvalidOperationException("Azure IMDS returned an empty access token.");

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