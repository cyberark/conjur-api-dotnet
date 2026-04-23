namespace Conjur.Test;

public class AzureAuthenticatorTest : Base
{
    // Azure IMDS token endpoint (used for mocking)
    private static readonly Uri ImdsTokenUri = new(
        "http://169.254.169.254/metadata/identity/oauth2/token" +
        "?api-version=2018-02-01" +
        $"&resource={Uri.EscapeDataString("https://management.azure.com/")}");

    [Test]
    public void AuthenticateTest()
    {
        // Check if we're running in an Azure environment. Set RUN_AZURE_TESTS=true in CI.
        //if (Environment.GetEnvironmentVariable("RUN_AZURE_TESTS") != "true")
        //{
        //    Assert.Ignore("This test requires an Azure environment to run. Set the RUN_AZURE_TESTS environment variable to 'true' to run this test.");
        //}

        const string conjurIdentity = "somehost";
        const string conjurAuthenticator = "authn-azure/test";

        // --- Mock Azure IMDS response ---
        const string fakeAzureJwt = "fake.azure.jwt";
        var imdsJson = $@"{{""access_token"":""{fakeAzureJwt}"",""token_type"":""Bearer"",""expires_in"":""3599"",""resource"":""https://management.azure.com/""}}";
        Mocker.Mock(ImdsTokenUri, imdsJson);

        // --- Mock Conjur authenticate response ---
        const string mockConjurToken = "conjur_token";
        var mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockConjurToken));
        var conjurEndpoint = new Uri(Client.ApplianceUri, $"authn-azure/test/test-account/{conjurIdentity}/authenticate");
        var m = Mocker.Mock(conjurEndpoint, mockTokenEncoded);

        m.Verifier = request =>
        {
            // Verify the request to Conjur contains the Azure JWT as form-encoded "jwt" field
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("application/x-www-form-urlencoded", request.Content.Headers.ContentType.MediaType);

            var body = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.IsTrue(body.Contains($"jwt={Uri.EscapeDataString(fakeAzureJwt)}"),
                $"Expected body to contain jwt field, got: {body}");
        };

        // Both mock clients share the same Mocker dictionary, so one GetMockHttpClient() suffices.
        var mockHttpClient = Mocker.GetMockHttpClient();

        var azureAuthenticator = new AzureAuthenticator(
            conjurHttpClient: mockHttpClient,
            imdsHttpClient: mockHttpClient,
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator
        );

        var token = azureAuthenticator.GetToken();
        Assert.AreEqual(mockConjurToken, token);
    }
}