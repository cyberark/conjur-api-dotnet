namespace Conjur.Test;

public class GCPAuthenticatorTest : Base
{
    private const string AuthnGcpPolicy = @"
- !variable
  id: enabled

- !variable
  id: provider-uri

- !webservice

- !group apps

- !permit
  role: !group apps
  privilege: [ read, authenticate ]
  resource: !webservice

- !grant
  role: !group apps
  member: !host /data/test/gcp-apps/test-app
";

    private const string AuthGcpRolesPolicyTemplate = @"
- !policy
  id: gcp-apps
  body:
  - &variables
    - !variable database/username
    - !variable database/password
  - !group secrets-users
  - !permit
    role: !group secrets-users
    privilege: [ read, execute ]
    resource: *variables

  - !group
  - !host
    id: test-app
    annotations:
      authn-jwt/gcp/google/compute_engine/project_id: {0}
  - !grant
    role: !group
    member: !host test-app
  - !grant
    member: !group
    role: !group secrets-users
";

    [Test]
    public async Task TestGcpAuthenticatorRefreshJwt_UsesExplicitToken()
    {
        var authenticator = new GCPAuthenticator(
            conjurHttpClient: Mocker.GetMockHttpClient(),
            gcpHttpClient: Mocker.GetMockHttpClient(),
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: "host/data/test/gcp-apps/test-app",
            jwt: "explicit-token");

        await authenticator.RefreshJwtAsync();
        Assert.AreEqual("explicit-token", authenticator.Jwt);
    }

    [Test]
    public void TestGcpAuthenticatorPassesExplicitJwtToConjur()
    {
        const string identity = "host/data/test/gcp-apps/test-app";
        const string explicitJwt = "explicit-gcp-token";
        const string expectedConjurToken = "gcp-conjur-token";

        // must match GCPAuthenticator default/authenticator route
        const string authenticator = "authn-jwt/gcp";

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedConjurToken));
        var conjurEndpoint = new Uri(
            Client.ApplianceUri,
            $"{authenticator}/{Client.AccountName}/{Uri.EscapeDataString(identity)}/authenticate");

        var m = Mocker.Mock(conjurEndpoint, encoded);
        m.Verifier = request =>
{
    var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
    Assert.IsTrue(body.Contains($"jwt={Uri.EscapeDataString(explicitJwt)}"),
        $"Expected jwt form field to contain explicit token. Actual body: {body}");
};

        var gcpAuthenticator = new GCPAuthenticator(
            conjurHttpClient: Mocker.GetMockHttpClient(),
            gcpHttpClient: Mocker.GetMockHttpClient(),
            conjurUri: Client.ApplianceUri.ToString(),
            conjurAccount: Client.AccountName,
            identity: identity,
            authenticator: authenticator,
            jwt: explicitJwt);

        var token = gcpAuthenticator.GetToken();
        Assert.AreEqual(expectedConjurToken, token);
    }

    [Test]
    public void AuthnGcp_MetadataOrExplicitToken()
    {
        if (Environment.GetEnvironmentVariable("TEST_GCP") != "true")
            Assert.Ignore("Skipping GCP authn test. Set TEST_GCP=true to run.");

        var gcpIdToken = Environment.GetEnvironmentVariable("GCP_ID_TOKEN");

        if (string.IsNullOrEmpty(gcpIdToken))
            Assert.Fail("GCP_ID_TOKEN must be set to run this test.");

        var adminClient = CreateAdminClient();

        EnsureDataTestPolicyExists(adminClient);
        EnsureAuthnGcpPolicyExists(adminClient);

        var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID") ?? "test-project";
        var rolesPolicy = string.Format(AuthGcpRolesPolicyTemplate, projectId);

        using var rolesPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(rolesPolicy));
        adminClient.Policy("data/test").LoadPolicy(rolesPolicyStream, WebRequestMethods.Http.Post);

        using var authnPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(AuthnGcpPolicy));
        adminClient.Policy("conjur/authn-jwt/gcp").LoadPolicy(authnPolicyStream, WebRequestMethods.Http.Post);

        adminClient.Variable("conjur/authn-jwt/gcp/enabled")
            .AddSecret(Encoding.UTF8.GetBytes("true"));

        adminClient.Variable("conjur/authn-jwt/gcp/provider-uri")
            .AddSecret(Encoding.UTF8.GetBytes("https://accounts.google.com"));

        adminClient.Variable("data/test/gcp-apps/database/username")
            .AddSecret(Encoding.UTF8.GetBytes("secret"));
        adminClient.Variable("data/test/gcp-apps/database/password")
            .AddSecret(Encoding.UTF8.GetBytes("P@ssw0rd!"));

        const string conjurIdentity = "host/data/test/gcp-apps/test-app";
        const string conjurAuthenticator = "authn-jwt/gcp";

        // Use metadata stub so tests can run outside a GCP agent.
        var metadataBase = "test:///gcp-identity";
        var audience = Uri.EscapeDataString($"conjur/{adminClient.AccountName}/host/data/test/gcp-apps/test-app");
        var metadataUri = new Uri($"{metadataBase}?audience={audience}&format=full");

        var metadataMock = Mocker.Mock(metadataUri, gcpIdToken);
        metadataMock.Verifier = request =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsTrue(request.Headers.TryGetValues(GCPAuthenticator.GcpMetadataFlavorHeaderName, out var values));
            Assert.AreEqual(GCPAuthenticator.GcpMetadataFlavorHeaderValue, values!.First());
        };

        var authenticator = new GCPAuthenticator(
            conjurHttpClient: new HttpClient(),
            gcpHttpClient: Mocker.GetMockHttpClient(),
            conjurUri: adminClient.ApplianceUri.ToString(),
            conjurAccount: adminClient.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator,
            gcpIdentityUrl: metadataBase);

        Console.WriteLine($"GCP authenticate URL: {authenticator.GetConjurAuthenticateUrl()}");

        var token = authenticator.GetToken();
        Assert.IsNotEmpty(token);

        var secret = adminClient.Variable("data/test/gcp-apps/database/username").GetValue();
        Assert.AreEqual("secret", secret);

        secret = adminClient.Variable("data/test/gcp-apps/database/password").GetValue();
        Assert.AreEqual("P@ssw0rd!", secret);
    }

    private static Client CreateAdminClient()
    {
        var applianceUrl = Environment.GetEnvironmentVariable("CONJUR_APPLIANCE_URL");
        var account = Environment.GetEnvironmentVariable("CONJUR_ACCOUNT");

        if (string.IsNullOrEmpty(applianceUrl) || string.IsNullOrEmpty(account))
            Assert.Fail("CONJUR_APPLIANCE_URL and CONJUR_ACCOUNT must be set to run GCP integration tests.");

        var netrcPath = Environment.GetEnvironmentVariable("CONJUR_NETRC_PATH")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".netrc");

        if (!File.Exists(netrcPath))
            Assert.Fail($"netrc file not found at '{netrcPath}'.");

        var entry = NetrcParser.FindEntry(netrcPath, applianceUrl);
        if (entry is null || string.IsNullOrEmpty(entry.Login) || string.IsNullOrEmpty(entry.Password))
            Assert.Fail($"No valid netrc entry found for '{applianceUrl}' in '{netrcPath}'.");

        var client = new Client(applianceUrl, account);
        client.Credential = new NetworkCredential(entry.Login, entry.Password);
        return client;
    }

    private static void EnsureDataTestPolicyExists(Client adminClient)
    {
        const string bootstrapPolicy = @"
- !policy
  id: data
  body:
  - !policy
    id: test
";
        using var bootstrapPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(bootstrapPolicy));
        adminClient.Policy("root").LoadPolicy(bootstrapPolicyStream, WebRequestMethods.Http.Post);
    }

    private static void EnsureAuthnGcpPolicyExists(Client adminClient)
    {
        const string bootstrapPolicy = @"
- !policy
  id: conjur
  body:
  - !policy
    id: authn-jwt
    body:
    - !policy
      id: gcp
";
        using var bootstrapPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(bootstrapPolicy));
        adminClient.Policy("root").LoadPolicy(bootstrapPolicyStream, WebRequestMethods.Http.Post);
    }

}