// <copyright file="GCPAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2026 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     GCP workload identity authenticator.
// </summary>

#nullable enable

namespace Conjur;

/// <summary>
/// GCP authenticator.
/// Authenticates to Conjur using a GCP identity JWT token.
/// </summary>
public class GCPAuthenticator : IAuthenticator, IDisposable
{
    public const string GcpMetadataFlavorHeaderName = "Metadata-Flavor";
    public const string GcpMetadataFlavorHeaderValue = "Google";
    public const string DefaultGcpIdentityUrl = "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/identity";

    private readonly HttpClient conjurHttpClient;
    private readonly HttpClient gcpHttpClient;
    private readonly Uri conjurApiUri;
    private readonly string conjurAccount;
    private readonly string identity;
    private readonly string authenticator;
    private readonly string gcpIdentityUrl;
    private readonly bool ownsConjurHttpClient;
    private readonly bool ownsGcpHttpClient;
    private readonly bool jwtProvidedAtConstruction;

    private bool disposed;

    /// <summary>
    /// Gets or sets the JWT token. If null/empty, one is fetched from GCP metadata.
    /// </summary>
    public string? Jwt { get; set; }

    public GCPAuthenticator(
        HttpClient conjurHttpClient,
        HttpClient gcpHttpClient,
        string conjurUri,
        string conjurAccount,
        string identity,
        string authenticator = "authn-jwt/gcp",
        string? jwt = null,
        string gcpIdentityUrl = DefaultGcpIdentityUrl)
        : this(
            conjurHttpClient,
            gcpHttpClient,
            conjurUri,
            conjurAccount,
            identity,
            authenticator,
            jwt,
            gcpIdentityUrl,
            ownsConjurHttpClient: false,
            ownsGcpHttpClient: false)
    {
    }

    public GCPAuthenticator(
        string conjurUri,
        string conjurAccount,
        string identity,
        string authenticator = "authn-jwt/gcp",
        string? jwt = null,
        string gcpIdentityUrl = DefaultGcpIdentityUrl)
        : this(
            new HttpClient(),
            new HttpClient(new HttpClientHandler { UseProxy = false }),
            conjurUri,
            conjurAccount,
            identity,
            authenticator,
            jwt,
            gcpIdentityUrl,
            ownsConjurHttpClient: true,
            ownsGcpHttpClient: true)
    {
    }

    private GCPAuthenticator(
        HttpClient conjurHttpClient,
        HttpClient gcpHttpClient,
        string conjurUri,
        string conjurAccount,
        string identity,
        string authenticator,
        string? jwt,
        string gcpIdentityUrl,
        bool ownsConjurHttpClient,
        bool ownsGcpHttpClient)
    {
        this.conjurHttpClient = conjurHttpClient;
        this.gcpHttpClient = gcpHttpClient;
        this.conjurApiUri = new Uri(conjurUri);
        this.conjurAccount = conjurAccount;
        this.identity = identity;
        this.authenticator = authenticator;
        this.Jwt = jwt;
        this.gcpIdentityUrl = gcpIdentityUrl;
        this.ownsConjurHttpClient = ownsConjurHttpClient;
        this.ownsGcpHttpClient = ownsGcpHttpClient;
        jwtProvidedAtConstruction = !string.IsNullOrWhiteSpace(jwt);
    }

    #region IAuthenticator implementation

    public string GetToken() => GetTokenAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await RefreshJwtAsync(cancellationToken);
        return await GetConjurTokenAsync(Jwt!, cancellationToken);
    }

    #endregion

    /// <summary>
    /// Refreshes JWT according to token source:
    /// - If no JWT was supplied at construction time, refreshes from GCP metadata on each call.
    /// - If JWT was explicitly supplied, keeps it unless forceRefresh is true or Jwt is currently empty.
    /// </summary>
    public Task RefreshJwtAsync(CancellationToken cancellationToken = default) =>
        RefreshJwtAsync(forceRefresh: false, cancellationToken);

    /// <summary>
    /// Refreshes JWT, optionally forcing metadata re-fetch.
    /// </summary>
    public async Task RefreshJwtAsync(bool forceRefresh, CancellationToken cancellationToken = default)
    {
        var shouldRefreshFromMetadata =
            forceRefresh ||
            string.IsNullOrWhiteSpace(Jwt) ||
            !jwtProvidedAtConstruction;

        if (!shouldRefreshFromMetadata)
        {
            return;
        }

        Jwt = await GetGcpJwtAsync(cancellationToken);
    }

    public string GetConjurAuthenticateUrl()
    {
        var conjId = Uri.EscapeDataString(identity);
        var apiUrl = conjurApiUri.ToString();
        apiUrl = apiUrl.EndsWith('/') ? apiUrl[..^1] : apiUrl;

        return $"{apiUrl}/{authenticator}/{conjurAccount}/{conjId}/authenticate";
    }

    private async Task<string> GetGcpJwtAsync(CancellationToken cancellationToken)
    {
        var audience = BuildAudience();
        var fullUrl = $"{gcpIdentityUrl}?audience={Uri.EscapeDataString(audience)}&format=full";

        using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        request.Headers.Add(GcpMetadataFlavorHeaderName, GcpMetadataFlavorHeaderValue);

        using var response = await gcpHttpClient.SendAsync(request, cancellationToken);
        var body = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GCP metadata token request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Url='{fullUrl}'. Body='{body}'");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("GCP metadata service returned an empty token.");
        }

        return body;
    }

    private string BuildAudience()
    {
        var normalizedIdentity = identity.StartsWith("host/", StringComparison.OrdinalIgnoreCase)
            ? identity["host/".Length..]
            : identity;

        return $"conjur/{conjurAccount}/host/{normalizedIdentity}";
    }

    private async Task<string> GetConjurTokenAsync(string gcpJwt, CancellationToken cancellationToken)
    {
        using var formContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("jwt", gcpJwt)
        ]);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetConjurAuthenticateUrl())
        {
            Content = formContent
        };
        httpRequestMessage.Headers.AcceptEncoding.TryParseAdd("base64");

        var stringBase64Response = await conjurHttpClient.SendAsync(httpRequestMessage, cancellationToken).ReadAsync(cancellationToken);

        var data = Convert.FromBase64String(stringBase64Response);
        return Encoding.UTF8.GetString(data);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        if (ownsConjurHttpClient)
            conjurHttpClient.Dispose();

        if (ownsGcpHttpClient)
            gcpHttpClient.Dispose();

        disposed = true;
        GC.SuppressFinalize(this);
    }
}