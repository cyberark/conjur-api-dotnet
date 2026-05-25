namespace Conjur.Test;

public class AzureAuthenticatorTest : Base
{
    private const string ImdsResource = "https://management.azure.com/";

    private static Uri ImdsTokenUri(string clientId = null)
    {
        var url = "http://169.254.169.254/metadata/identity/oauth2/token" +
                  "?api-version=2018-02-01" +
                  $"&resource={Uri.EscapeDataString(ImdsResource)}";
        if (!string.IsNullOrEmpty(clientId))
            url += $"&client_id={Uri.EscapeDataString(clientId)}";
        return new Uri(url);
    }

    /// <summary>
    /// Verifies that when an explicit JWT is provided it is forwarded
    /// to the Conjur authenticate endpoint unchanged.
    /// Mirrors Go's TestAzureAuthenticatorRefreshJWT.
    /// </summary>
    [Test]
    public void TestAzureAuthenticatorPassesJwtToConjur()
    {
        const string conjurIdentity = "somehost";
        const string conjurAuthenticator = "authn-azure/test";
        const string fakeAzureJwt = "explicit-token";

        Mocker.Mock(ImdsTokenUri(), $@"{{""access_token"":""{fakeAzureJwt}"",""token_type"":""Bearer"",""expires_in"":""3599"",""resource"":""{ImdsResource}""}}");

        const string mockConjurToken = "fake-token";
        var mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockConjurToken));
        var conjurEndpoint = new Uri(Client.ApplianceUri, $"authn-azure/test/test-account/{conjurIdentity}/authenticate");
        var m = Mocker.Mock(conjurEndpoint, mockTokenEncoded);

        m.Verifier = request =>
        {
            var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsTrue(body.Contains($"jwt={Uri.EscapeDataString(fakeAzureJwt)}"),
                $"Expected jwt field to contain explicit token, got: {body}");
        };

        var mockHttpClient = Mocker.GetMockHttpClient();
        var authenticator = new AzureAuthenticator(
            conjurHttpClient: mockHttpClient,
            imdsHttpClient: mockHttpClient,
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator
        );

        var token = authenticator.GetToken();
        Assert.AreEqual(mockConjurToken, token);
    }

    /// <summary>
    /// Verifies that the IMDS request does not include a client_id
    /// when no client ID is configured (system-assigned identity).
    /// Mirrors Go's TestAzureTokenRequest "creates a valid request when client ID is empty".
    /// </summary>
    [Test]
    public void TestAzureTokenRequest_NoClientId_OmitsClientIdParam()
    {
        const string conjurIdentity = "somehost";
        const string conjurAuthenticator = "authn-azure/test";
        const string fakeAzureJwt = "fake.azure.jwt";

        Uri capturedImdsUri = null;
        Mocker.Mock(ImdsTokenUri(), $@"{{""access_token"":""{fakeAzureJwt}"",""token_type"":""Bearer"",""expires_in"":""3599"",""resource"":""{ImdsResource}""}}")
            .Verifier = request => { capturedImdsUri = request.RequestUri; };

        var mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("conjur_token"));
        var conjurEndpoint = new Uri(Client.ApplianceUri, $"authn-azure/test/test-account/{conjurIdentity}/authenticate");
        Mocker.Mock(conjurEndpoint, mockTokenEncoded);

        var mockHttpClient = Mocker.GetMockHttpClient();
        var authenticator = new AzureAuthenticator(
            conjurHttpClient: mockHttpClient,
            imdsHttpClient: mockHttpClient,
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator
        );

        authenticator.GetToken();

        Assert.IsNotNull(capturedImdsUri);
        StringAssert.Contains($"resource={Uri.EscapeDataString(ImdsResource)}", capturedImdsUri.Query);
        StringAssert.DoesNotContain("client_id=", capturedImdsUri.Query);
    }

    /// <summary>
    /// Verifies that the IMDS request includes a client_id query parameter
    /// when a user-assigned identity client ID is configured.
    /// Mirrors Go's TestAzureTokenRequest "creates a valid request when client ID is provided".
    /// </summary>
    [Test]
    public void TestAzureTokenRequest_WithClientId_IncludesClientIdParam()
    {
        const string conjurIdentity = "somehost";
        const string conjurAuthenticator = "authn-azure/test";
        const string fakeAzureJwt = "fake.azure.jwt";
        const string testClientId = "test-client-id";

        Uri capturedImdsUri = null;
        Mocker.Mock(ImdsTokenUri(testClientId), $@"{{""access_token"":""{fakeAzureJwt}"",""token_type"":""Bearer"",""expires_in"":""3599"",""resource"":""{ImdsResource}""}}")
            .Verifier = request => { capturedImdsUri = request.RequestUri; };

        var mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("conjur_token"));
        var conjurEndpoint = new Uri(Client.ApplianceUri, $"authn-azure/test/test-account/{conjurIdentity}/authenticate");
        Mocker.Mock(conjurEndpoint, mockTokenEncoded);

        var mockHttpClient = Mocker.GetMockHttpClient();
        var authenticator = new AzureAuthenticator(
            conjurHttpClient: mockHttpClient,
            imdsHttpClient: mockHttpClient,
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator,
            clientId: testClientId
        );

        authenticator.GetToken();

        Assert.IsNotNull(capturedImdsUri);
        StringAssert.Contains($"resource={Uri.EscapeDataString(ImdsResource)}", capturedImdsUri.Query);
        StringAssert.Contains($"client_id={Uri.EscapeDataString(testClientId)}", capturedImdsUri.Query);
    }
}