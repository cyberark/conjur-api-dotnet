namespace Conjur.Test;

public class AzureAuthenticatorIntegrationTest : Base
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

    private const string AuthnAzurePolicy = @"
- !policy
  id: prod
  body:
  - !webservice

  - !variable
    id: provider-uri

  - !group apps

  - !permit
    role: !group apps
    privilege: [ read, authenticate ]
    resource: !webservice

  - !grant
    role: !group apps
    member: !host /data/test/azure-apps/azureVM
";

    private const string AuthAzureRolesPolicyTemplate = @"
- !policy
  id: azure-apps
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
    id: azureVM
    annotations:
      authn-azure/subscription-id: {0}
      authn-azure/resource-group: {1}
{2}
  - !grant
    role: !group
    member: !host azureVM
  - !grant
    member: !group
    role: !group secrets-users
";

    /// <summary>
    /// Creates a real authenticated Conjur admin client using the netrc file.
    /// Reads CONJUR_APPLIANCE_URL and CONJUR_ACCOUNT from environment variables.
    /// Credentials are read from the file at CONJUR_NETRC_PATH, or ~/.netrc if
    /// CONJUR_NETRC_PATH is not set (mounted at /root/.netrc inside the container
    /// by build.sh).
    /// </summary>
    private static Client CreateAdminClient()
    {
        var applianceUrl = Environment.GetEnvironmentVariable("CONJUR_APPLIANCE_URL");
        var account = Environment.GetEnvironmentVariable("CONJUR_ACCOUNT");

        if (string.IsNullOrEmpty(applianceUrl) || string.IsNullOrEmpty(account))
            Assert.Fail("CONJUR_APPLIANCE_URL and CONJUR_ACCOUNT must be set to run Azure integration tests.");

        var netrcPath = Environment.GetEnvironmentVariable("CONJUR_NETRC_PATH")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".netrc");

        if (!File.Exists(netrcPath))
            Assert.Fail($"netrc file not found at '{netrcPath}'. " +
                        "Ensure the agent's ~/.netrc is mounted into the container by build.sh.");

        var entry = NetrcParser.FindEntry(netrcPath, applianceUrl);
        if (entry is null || string.IsNullOrEmpty(entry.Login) || string.IsNullOrEmpty(entry.Password))
            Assert.Fail($"No valid netrc entry found for '{applianceUrl}' in '{netrcPath}'.");

        var client = new Client(applianceUrl, account);
        client.Credential = new System.Net.NetworkCredential(entry.Login, entry.Password);
        return client;
    }

    /// <summary>
    /// Integration test: authn-azure with system-assigned managed identity.
    /// </summary>
    [Test]
    public void AuthnAzure_SystemAssignedIdentity()
    {
        if (Environment.GetEnvironmentVariable("RUN_AZURE_TESTS") != "true")
            Assert.Ignore("Skipping Azure authn test. Set RUN_AZURE_TESTS=true to run.");

        Console.WriteLine("Azure test: starting AuthnAzure_SystemAssignedIdentity");

        var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        var resourceGroup = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP");

        if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroup))
        {
            Assert.Fail("AZURE_SUBSCRIPTION_ID and AZURE_RESOURCE_GROUP must be set to run this test.");
        }

        var adminClient = CreateAdminClient();

        const string conjurIdentity = "host/data/test/azure-apps/azureVM";
        const string conjurAuthenticator = "authn-azure/prod";

        EnsureDataTestPolicyExists(adminClient);
        EnsureAuthnAzurePolicyExists(adminClient);

        var rolesPolicy = string.Format(AuthAzureRolesPolicyTemplate,
            subscriptionId,
            resourceGroup,
            string.Empty);

        using var rolesPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(rolesPolicy));
        adminClient.Policy("data/test").LoadPolicy(rolesPolicyStream, WebRequestMethods.Http.Post);

        using var authnPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(AuthnAzurePolicy));
        adminClient.Policy("conjur/authn-azure").LoadPolicy(authnPolicyStream, WebRequestMethods.Http.Post);

        adminClient.Variable("conjur/authn-azure/prod/provider-uri")
            .AddSecret(Encoding.UTF8.GetBytes("https://sts.windows.net/df242c82-fe4a-47e0-b0f4-e3cb7f8104f1/"));
        adminClient.Variable("data/test/azure-apps/database/username")
            .AddSecret(Encoding.UTF8.GetBytes("secret"));
        adminClient.Variable("data/test/azure-apps/database/password")
            .AddSecret(Encoding.UTF8.GetBytes("P@ssw0rd!"));

        var authenticator = new AzureAuthenticator(
            conjurUri: adminClient.ApplianceUri.ToString(),
            conjurAccount: adminClient.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator
        );

        var token = authenticator.GetToken();
        Assert.IsNotEmpty(token);

        var secret = adminClient.Variable("data/test/azure-apps/database/username").GetValue();
        Assert.AreEqual("secret", secret);

        secret = adminClient.Variable("data/test/azure-apps/database/password").GetValue();
        Assert.AreEqual("P@ssw0rd!", secret);

        Console.WriteLine("Azure test: AuthnAzure_SystemAssignedIdentity PASSED");
    }

    /// <summary>
    /// Integration test: authn-azure with user-assigned managed identity.
    /// </summary>
    [Test]
    public void AuthnAzure_UserAssignedIdentity()
    {
        if (Environment.GetEnvironmentVariable("RUN_AZURE_TESTS") != "true")
            Assert.Ignore("Skipping Azure authn test. Set RUN_AZURE_TESTS=true to run.");

        var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        var resourceGroup = Environment.GetEnvironmentVariable("AZURE_RESOURCE_GROUP");
        var userAssignedIdentity = Environment.GetEnvironmentVariable("USER_ASSIGNED_IDENTITY");
        var userAssignedIdentityClientId = Environment.GetEnvironmentVariable("USER_ASSIGNED_IDENTITY_CLIENT_ID");

        if (string.IsNullOrEmpty(subscriptionId) ||
            string.IsNullOrEmpty(resourceGroup) ||
            string.IsNullOrEmpty(userAssignedIdentity) ||
            string.IsNullOrEmpty(userAssignedIdentityClientId))
        {
            Assert.Fail("AZURE_SUBSCRIPTION_ID, AZURE_RESOURCE_GROUP, USER_ASSIGNED_IDENTITY, USER_ASSIGNED_IDENTITY_CLIENT_ID must be set to run user-assigned Azure test.");
        }

        Console.WriteLine("Azure test: starting AuthnAzure_UserAssignedIdentity");

        var adminClient = CreateAdminClient();

        const string conjurIdentity = "host/data/test/azure-apps/azureVM";
        const string conjurAuthenticator = "authn-azure/prod";

        EnsureDataTestPolicyExists(adminClient);
        EnsureAuthnAzurePolicyExists(adminClient);

        var userIdentityAnnotation = $"      authn-azure/user-assigned-identity: \"{userAssignedIdentity}\"";
        var rolesPolicy = string.Format(AuthAzureRolesPolicyTemplate, subscriptionId, resourceGroup, userIdentityAnnotation);

        using var rolesPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(rolesPolicy));
        adminClient.Policy("data/test").LoadPolicy(rolesPolicyStream, WebRequestMethods.Http.Post);

        using var authnPolicyStream = new MemoryStream(Encoding.UTF8.GetBytes(AuthnAzurePolicy));
        adminClient.Policy("conjur/authn-azure").LoadPolicy(authnPolicyStream, WebRequestMethods.Http.Post);

        var providerUri = ResolveProviderUriFromImds(userAssignedIdentityClientId);
        adminClient.Variable("conjur/authn-azure/prod/provider-uri")
            .AddSecret(Encoding.UTF8.GetBytes(providerUri));
        adminClient.Variable("data/test/azure-apps/database/username")
            .AddSecret(Encoding.UTF8.GetBytes("secret"));
        adminClient.Variable("data/test/azure-apps/database/password")
            .AddSecret(Encoding.UTF8.GetBytes("P@ssw0rd!"));

        var authenticator = new AzureAuthenticator(
            conjurUri: adminClient.ApplianceUri.ToString(),
            conjurAccount: adminClient.AccountName,
            identity: conjurIdentity,
            authenticator: conjurAuthenticator,
            clientId: userAssignedIdentityClientId
        );

        var token = authenticator.GetToken();
        Assert.IsNotEmpty(token);

        var secret = adminClient.Variable("data/test/azure-apps/database/username").GetValue();
        Assert.AreEqual("secret", secret);

        secret = adminClient.Variable("data/test/azure-apps/database/password").GetValue();
        Assert.AreEqual("P@ssw0rd!", secret);

        Console.WriteLine("Azure test: AuthnAzure_UserAssignedIdentity PASSED");
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
        adminClient.Policy("root").LoadPolicy(bootstrapPolicyStream, "PUT");
    }

    private static void EnsureAuthnAzurePolicyExists(Client adminClient)
    {
        const string bootstrapAuthnAzurePolicy = @"
- !policy
  id: conjur
  body:
  - !policy
    id: authn-azure
";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(bootstrapAuthnAzurePolicy));
        adminClient.Policy("root").LoadPolicy(stream, WebRequestMethods.Http.Post);
    }

    private static string ResolveProviderUriFromImds(string clientId = null)
    {
        var url = "http://169.254.169.254/metadata/identity/oauth2/token" +
                  "?api-version=2018-02-01" +
                  $"&resource={Uri.EscapeDataString(ImdsResource)}";

        if (!string.IsNullOrEmpty(clientId))
            url += $"&client_id={Uri.EscapeDataString(clientId)}";

        using var http = new HttpClient(new HttpClientHandler { UseProxy = false });
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Metadata", "true");

        using var res = http.Send(req);
        res.EnsureSuccessStatusCode();

        var json = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = System.Text.Json.JsonDocument.Parse(json);

        var jwt = doc.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrEmpty(jwt))
            Assert.Fail("IMDS returned empty access_token.");

        var issuer = ExtractIssuer(jwt);
        return issuer.EndsWith("/", StringComparison.Ordinal) ? issuer : issuer + "/";
    }

    private static string ExtractIssuer(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            Assert.Fail("Invalid JWT format from IMDS.");

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        using var doc = System.Text.Json.JsonDocument.Parse(bytes);

        if (!doc.RootElement.TryGetProperty("iss", out var iss) || string.IsNullOrEmpty(iss.GetString()))
            Assert.Fail("JWT does not contain 'iss' claim.");

        return iss.GetString();
    }
}