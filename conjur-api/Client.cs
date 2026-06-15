// <copyright file="Client.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Base Conjur client class implementation.
// </summary>

using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Conjur;

/// <summary>
/// Conjur API client.
/// </summary>
public partial class Client
{
    private readonly string actingAs;
    private bool disableCertCheck;
    internal readonly HttpClient httpClient;
    private static string integrationName = "SecretsManagerDotNet SDK";
    private static string integrationType = "cybr-secretsmanager";
    private static string integrationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    private static string vendorName = "CyberArk";

    private static string vendorVersion = null;
    private static string telemetryHeader = null;

    /// <summary>
    /// Gets or sets the name of the integration. 
    /// When set, it also resets the cached telemetry header so it can be rebuilt.
    /// </summary>
    /// <value>
    /// The name of the integration.
    /// </value>
    public static string IntegrationName
    {
        get => integrationName;
        set
        {
            integrationName = value;
            telemetryHeader = null;  
        }
    }

    /// <summary>
    /// Gets or sets the type of the integration. 
    /// When set, it also resets the cached telemetry header so it can be rebuilt.
    /// </summary>
    /// <value>
    /// The type of the integration.
    /// </value>
    public static string IntegrationType
    {
        get => integrationType;
        set
        {
            integrationType = value;
            telemetryHeader = null;  
        }
    }

    /// <summary>
    /// Gets or sets the version of the integration.
    /// When set, it also resets the cached telemetry header so it can be rebuilt.
    /// </summary>
    /// <value>
    /// The version of the integration.
    /// </value>
    public static string IntegrationVersion
    {
        get => integrationVersion;
        set
        {
            integrationVersion = value;
            telemetryHeader = null;
        }
    }

    /// <summary>
    /// Gets or sets the name of the vendor.
    /// When set, it also resets the cached telemetry header so it can be rebuilt.
    /// </summary>
    /// <value>
    /// The name of the vendor.
    /// </value>
    public static string VendorName
    {
        get => vendorName;
        set
        {
            vendorName = value;
            telemetryHeader = null;
        }
    }

    /// <summary>
    /// Gets or sets the version of the vendor.
    /// When set, it also resets the cached telemetry header so it can be rebuilt.
    /// </summary>
    /// <value>
    /// The version of the vendor.
    /// </value>
    public static string VendorVersion
    {
        get => vendorVersion;
        set
        {
            vendorVersion = value;
            telemetryHeader = null;
        }
    }

    /// <summary>
    /// Constructs and returns the telemetry header string based on the integration and vendor information.
    /// The header is encoded in Base64 and uses URL-safe characters (`-` and `_`).
    /// If the telemetry header has been previously generated, it will be cached and reused.
    /// </summary>
    /// <returns>
    /// A Base64 encoded string representing the telemetry header, formatted as a URL-safe string.
    /// </returns>
    public static string GetTelemetryHeader(){
        var sb = new StringBuilder();
        if( telemetryHeader != null )
        {
            return telemetryHeader;
        }
        telemetryHeader = "";
        if( integrationName != null )
        {
            sb.Append("in=").Append(integrationName);
            if (integrationType != null) 
            {
                sb.Append("&it=").Append(integrationType); 
            }
            if (integrationVersion != null) 
            {
                sb.Append("&iv=").Append(integrationVersion); 
            }
        }
        if( vendorName != null )
        {
            sb.Append("&vn=").Append(vendorName);
            if (vendorVersion != null) 
            {
                sb.Append("&vv=").Append(vendorVersion); 
            }
        }
        telemetryHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return telemetryHeader;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Conjur.Client"/> class.
    /// </summary>
    /// <param name="applianceUri">Appliance URI.</param>
    /// <param name="account">Conjur account.</param>
    public Client(string applianceUri, string account)
    {
        AccountName = account;
        ApplianceUri = NormalizeBaseUri(applianceUri);
        TrustedCertificates = [];

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidateCertificate
        };

        httpClient = new HttpClient(httpClientHandler)
        {
            Timeout = TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().HttpRequestTimeout)
        };
        httpClient.DefaultRequestHeaders.Add("x-cybr-telemetry", GetTelemetryHeader());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Conjur.Client"/> class with mTLS support.
    /// </summary>
    /// <param name="applianceUri">Appliance URI.</param>
    /// <param name="account">Conjur account.</param>
    /// <param name="clientCertificate">Client certificate for mTLS.</param>
    public Client(string applianceUri, string account, X509Certificate2 clientCertificate)
    {
        AccountName = account;
        ApplianceUri = NormalizeBaseUri(applianceUri);
        TrustedCertificates = [];

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidateCertificate
        };

        // Add the client certificate for mTLS
        if (clientCertificate != null)
        {
            httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            httpClientHandler.ClientCertificates.Add(clientCertificate);
        }

        httpClient = new HttpClient(httpClientHandler)
        {
            Timeout = TimeSpan.FromMilliseconds(ApiConfigurationManager.GetInstance().HttpRequestTimeout)
        };
        httpClient.DefaultRequestHeaders.Add("x-cybr-telemetry", GetTelemetryHeader());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Conjur.Client"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for requests.</param>
    /// <param name="applianceUri">Appliance URI.</param>
    /// <param name="account">Conjur account.</param>
    public Client(HttpClient httpClient, string applianceUri, string account)
    {
        this.httpClient = httpClient;
        AccountName = account;
        ApplianceUri = NormalizeBaseUri(applianceUri);
        TrustedCertificates = [];
    }

    internal Client(Client other, string role) : this(other.ApplianceUri.AbsoluteUri, other.AccountName)
    {
        actingAs = role;
        httpClient = other.httpClient;
        Authenticator = other.Authenticator;
    }

    /// <summary>
    /// Disables SSL Cert check. Can be used when Conjur is configured with self-signed cert.
    /// </summary>
    /// <remarks>
    /// Warning: this is a security risk and should be used only for testing purposes.
    /// </remarks>
    public void DisableCertCheck()
    {
        disableCertCheck = true;
    }

    /// <summary>
    /// Enables SSL Cert check. This is already the default. This method is only necessary
    /// if <see cref="DisableCertCheck"/> was called before.
    /// </summary>
    public void EnableCertCheck()
    {
        disableCertCheck = false;
    }

    /// <summary>
    /// Gets the appliance URI.
    /// </summary>
    /// <value>The appliance URI.</value>
    public Uri ApplianceUri { get; }

    /// <summary>
    /// Gets the name of the Conjur organization account.
    /// </summary>
    /// <returns>The account name.</returns>
    public string AccountName { get; }

    /// <summary>
    /// Gets or sets the authenticator used to establish Conjur identity.
    /// This gets automatically set by setting <see cref="Client.Credential"/>.
    /// </summary>
    /// <value>The authenticator.</value>
    public IAuthenticator Authenticator { get; set; }

    /// <summary>
    /// Sets the username and API key to authenticate.
    /// This initializes <see cref="Client.Authenticator"/>.
    /// Use <see cref="Client.LogIn(NetworkCredential)"/> to use a password.
    /// </summary>
    /// <value>The credential of username and API key, where username is
    /// for example "bob" or "host/jenkins".</value>
    public NetworkCredential Credential
    {
        set => Authenticator = new ApiKeyAuthenticator(new Uri(ApplianceUri + "authn"), AccountName, value, this);
    }

    /// <summary>
    /// Gets the collection of extra certificates trusted for authenticating the
    /// Conjur server in addition to the system ones.
    /// </summary>
    /// <value>The trusted certificates collection.</value>
    public X509Certificate2Collection TrustedCertificates
    {
        get;
    }

    /// <summary>
    /// Logs in using a password. Sets <see cref="Authenticator"/>
    /// <seealso cref="Credential"/>
    /// </summary>
    /// <returns>The API key.</returns>
    /// <param name="userName">Username to log in as (for example "bob"
    /// or "host/example.com").</param>
    /// <param name="password">Password of the user.</param>
    public string LogIn(string userName, string password)
    {
        return LogIn(new NetworkCredential(userName, password));
    }

    /// <summary>
    /// Logs in using a password. Sets <see cref="Authenticator"/>
    /// <seealso cref="Credential"/>
    /// </summary>
    /// <returns>The API key.</returns>
    /// <param name="userName">Username to log in as (for example "bob"
    /// or "host/example.com").</param>
    /// <param name="password">Password of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public Task<string> LogInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        return LogInAsync(new NetworkCredential(userName, password), cancellationToken);
    }

    /// <summary>
    /// Logs in using a password. Sets <see cref="Authenticator"/>
    /// <seealso cref="Credential"/>
    /// </summary>
    /// <returns>The API key.</returns>
    /// <param name="credential">The credential of username and password,
    /// where username is for example "bob" or "host/jenkins".</param>
    public string LogIn(NetworkCredential credential)
    {
        var request = Request($"authn/{AccountName}/login");

        // there seems to be no sane way to force HttpRequestMessage to authenticate
        // properly by itself, so generate the header manually
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential.UserName + ":" + credential.Password));
        request.Headers.Add("Authorization", "Basic " + auth);
        var apiKey = httpClient.Send(request).Read();

        Credential = new NetworkCredential(credential.UserName, apiKey);
        return apiKey;
    }

    /// <summary>
    /// Logs in using a password. Sets <see cref="Authenticator"/>
    /// <seealso cref="Credential"/>
    /// </summary>
    /// <returns>The API key.</returns>
    /// <param name="credential">The credential of username and password,
    /// where username is for example "bob" or "host/jenkins".</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public async Task<string> LogInAsync(NetworkCredential credential, CancellationToken cancellationToken = default)
    {
        var request = Request($"authn/{AccountName}/login");

        // there seems to be no sane way to force HttpRequestMessage to authenticate
        // properly by itself, so generate the header manually
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential.UserName + ":" + credential.Password));
        request.Headers.Add("Authorization", "Basic " + auth);
        var apiKey = await httpClient.SendAsync(request, cancellationToken).ReadAsync(cancellationToken);

        Credential = new NetworkCredential(credential.UserName, apiKey);
        return apiKey;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal HttpResponseMessage Send(HttpRequestMessage request) => httpClient.Send(request);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => httpClient.SendAsync(request, cancellationToken);

    /// <summary>
    /// Create an HttpRequestMessage for the specified path.
    /// </summary>
    /// <param name="path">Path, NOT including the leading slash.</param>
    /// <returns>An HttpRequestMessage for the specified appliance path.</returns>
    internal HttpRequestMessage Request(string path)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(ApplianceUri + path)
        };
        return httpRequestMessage;
    }

    /// <summary>
    /// Create an authenticated HttpRequestMessage for the specified path.
    /// </summary>
    /// <param name="path">Path, NOT including the leading slash.</param>
    /// <returns>An HttpRequestMessage for the specified appliance path, with
    /// authorization header set using <see cref="Authenticator"/>.</returns>
    internal HttpRequestMessage AuthenticatedRequest(string path)
    {
        if (actingAs is not null)
        {
            path += (path.Contains('?') ? "&" : "?") + $"acting_as={Uri.EscapeDataString(actingAs)}";
        }

        return ApplyAuthentication(Request(path));
    }

    /// <summary>
    /// Create an authenticated HttpRequestMessage for the specified path.
    /// </summary>
    /// <param name="path">Path, NOT including the leading slash.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>An HttpRequestMessage for the specified appliance path, with
    /// authorization header set using <see cref="Authenticator"/>.</returns>
    internal Task<HttpRequestMessage> AuthenticatedRequestAsync(string path, CancellationToken cancellationToken)
    {
        if (actingAs is not null)
        {
            path += (path.Contains('?') ? "&" : "?") + $"acting_as={Uri.EscapeDataString(actingAs)}";
        }

        return ApplyAuthenticationAsync(Request(path), cancellationToken);
    }

    /// <summary>
    /// Normalizes the base URI, removing double slashes and adding a trailing
    /// slash, as necessary.
    /// </summary>
    /// <returns>The normalized base URI.</returns>
    /// <param name="uri">Base appliance URI to normalize.</param>
    internal static Uri NormalizeBaseUri(string uri)
    {
        var uriBuilder = new UriBuilder(uri);
        // appliance's nginx doesn't like double slashes,
        // eg. it returns 401 on https://example.org//api/info

        // so normalize to remove multiple slashes
        var normalizedPath = MultipleSlashesPattern().Replace(uriBuilder.Path, "/");

        // make sure there is a trailing slash
        uriBuilder.Path = normalizedPath.EndsWith('/') ? normalizedPath : normalizedPath + "/";

        return uriBuilder.Uri;
    }

    [GeneratedRegex("//+")]
    private static partial Regex MultipleSlashesPattern();

    /// <summary>
    /// Loads a client certificate from file paths (PEM format).
    /// </summary>
    /// <param name="certPath">Path to the PEM-encoded client certificate file.</param>
    /// <param name="keyPath">Path to the PEM-encoded private key file (optional if cert contains both).</param>
    /// <returns>X509Certificate2 containing the certificate and private key.</returns>
    public static X509Certificate2 LoadClientCertificateFromFiles(string certPath, string keyPath = null)
    {
        if (string.IsNullOrEmpty(certPath))
        {
            throw new ArgumentNullException(nameof(certPath));
        }

        var certData = File.ReadAllBytes(certPath);
        byte[] keyData = null;

        if (!string.IsNullOrEmpty(keyPath))
        {
            keyData = File.ReadAllBytes(keyPath);
        }

        return LoadClientCertificateFromPem(certData, keyData);
    }

    /// <summary>
    /// Loads a client certificate from PEM-encoded strings.
    /// </summary>
    /// <param name="certPem">PEM-encoded certificate data.</param>
    /// <param name="keyPem">PEM-encoded private key data (optional if cert contains both).</param>
    /// <returns>X509Certificate2 containing the certificate and private key.</returns>
    public static X509Certificate2 LoadClientCertificateFromPem(byte[] certPem, byte[] keyPem = null)
    {
        if (certPem == null || certPem.Length == 0)
        {
            throw new ArgumentNullException(nameof(certPem));
        }

        try
        {
            var certStr = Encoding.UTF8.GetString(certPem);
            X509Certificate2 ephemeral;

            if (keyPem != null && keyPem.Length > 0)
            {
                var keyStr = Encoding.UTF8.GetString(keyPem);
                ephemeral = X509Certificate2.CreateFromPem(certStr, keyStr);
            }
            else
            {
                ephemeral = X509Certificate2.CreateFromPem(certStr);
            }

            // CreateFromPem uses an ephemeral key; re-import via PKCS#12 so SChannel on Windows
            // can use the certificate for TLS client authentication.
            return PersistCertificateForClientAuth(ephemeral);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to load client certificate from PEM data", ex);
        }
    }

    private static X509Certificate2 PersistCertificateForClientAuth(X509Certificate2 ephemeral)
    {
        try
        {
            var pfxBytes = ephemeral.Export(X509ContentType.Pkcs12);
            return new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.UserKeySet);
        }
        finally
        {
            ephemeral.Dispose();
        }
    }

    /// <summary>
    /// Loads a client certificate from PEM-encoded strings.
    /// </summary>
    /// <param name="certPem">PEM-encoded certificate data as string.</param>
    /// <param name="keyPem">PEM-encoded private key data as string (optional).</param>
    /// <returns>X509Certificate2 containing the certificate and private key.</returns>
    public static X509Certificate2 LoadClientCertificateFromPem(string certPem, string keyPem = null)
    {
        if (string.IsNullOrEmpty(certPem))
        {
            throw new ArgumentNullException(nameof(certPem));
        }

        var certBytes = Encoding.UTF8.GetBytes(certPem);
        byte[] keyBytes = null;

        if (!string.IsNullOrEmpty(keyPem))
        {
            keyBytes = Encoding.UTF8.GetBytes(keyPem);
        }

        return LoadClientCertificateFromPem(certBytes, keyBytes);
    }

    /// <summary>
    /// Validates the Conjur appliance certificate.
    /// <see cref="RemoteCertificateValidationCallback"/>
    /// </summary>
    /// <returns><c>true</c>, if certificate was valid, <c>false</c> otherwise.</returns>
    /// <param name="sender">Sender of the validation request.</param>
    /// <param name="certificate">Certificate to be validated.</param>
    /// <param name="chain">Certificate chain, as resolved by the system.</param>
    /// <param name="sslPolicyErrors">SSL policy errors from the system.</param>
    private bool ValidateCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (disableCertCheck)
        {
            return true;
        }

        return sslPolicyErrors switch
        {
            SslPolicyErrors.RemoteCertificateChainErrors => chain.VerifyWithExtraRoots(certificate, TrustedCertificates),
            SslPolicyErrors.None => true,
            _ => false
        };
    }

    private HttpRequestMessage ApplyAuthentication(HttpRequestMessage request)
    {
        if (Authenticator is null)
        {
            throw new InvalidOperationException("Authentication required.");
        }

        var authenticatorToken = Authenticator.GetToken();
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticatorToken));

        request.Headers.Add("Authorization", "Token token=\"" + token + "\"");
        return request;
    }

    private async Task<HttpRequestMessage> ApplyAuthenticationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Authenticator is null)
        {
            throw new InvalidOperationException("Authentication required.");
        }

        var authenticatorToken = await Authenticator.GetTokenAsync(cancellationToken);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticatorToken));

        request.Headers.Add("Authorization", "Token token=\"" + token + "\"");
        return request;
    }
}
