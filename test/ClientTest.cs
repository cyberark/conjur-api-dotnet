namespace Conjur.Test;

public class ClientTest : Base
{
    [Test]
    public void TestInfo()
    {
        Assert.AreEqual(TestAccount, Client.AccountName);
    }

    [Test]
    public async Task TestLogin()
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/" + TestAccount + "/login"), "api-key").Verifier =
            (HttpRequestMessage requestMessage) => Assert.AreEqual("Basic YWRtaW46c2VjcmV0", requestMessage.Headers.GetValues("Authorization").SingleOrDefault());

        var apiKey = Client.LogIn("admin", "secret");
        Assert.AreEqual("api-key", apiKey);
        VerifyAuthenticator(Client.Authenticator);

        apiKey = await Client.LogInAsync("admin", "secret");
        Assert.AreEqual("api-key", apiKey);
        VerifyAuthenticator(Client.Authenticator);
    }

    private void VerifyAuthenticator(IAuthenticator authenticator)
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token")
            .Verifier = (HttpRequestMessage requestMessage) =>
        {
            Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
            Assert.AreEqual("api-key", requestMessage.Content.ReadAsStringAsync().Result);
        };
        Assert.AreEqual("token", authenticator.GetToken());
    }

    [Test]
    public void TestAuthenticatedRequest()
    {
        Mocker.Mock(new Uri("test://info.com"), "{ \"account\": \"test-account\" }");
        Client.Authenticator = new MockAuthenticator();
        var testRequest = Client.AuthenticatedRequest("info");
        Assert.AreEqual("Token token=\"dG9rZW4=\"", // "token" base64ed
            testRequest.Headers.GetValues("Authorization").Single());

        Client.Authenticator = null;
        Assert.Throws<InvalidOperationException>(() => Client.AuthenticatedRequest("info"));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Client.AuthenticatedRequestAsync("info", CancellationToken.None));
    }

    [Test]
    public async Task ActingAsTest()
    {
        // Test in mono fails when using : in role variable. role should be TestAccount:Kind:foo
        const string role = "foo";
        var resourceVarUri = $"{BaseUri}resources/{TestAccount}/{Constants.KIND_VARIABLE}";
        string[] expected = [$"{Client.AccountName}:{Constants.KIND_VARIABLE}:id"];

        Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000&acting_as={role}"), $"[{{\"id\":\"{Client.AccountName}:{Constants.KIND_VARIABLE}:id\"}}]");
        Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000"), "[]");

        Client.Authenticator = new MockAuthenticator();

        var actingAsClientVars = Client.ActingAs(role).ListVariables(null, 1000, 0).ToList();
        var plainClientVars = Client.ListVariables(null, 1000, 0).ToList();

        CollectionAssert.AreEqual(expected, actingAsClientVars.Select(x => x.Id).ToArray());
        CollectionAssert.IsEmpty(plainClientVars);

        actingAsClientVars = await Client.ActingAs(role).ListVariablesAsync(null, 1000, 0).ToListAsync();
        plainClientVars = await Client.ListVariablesAsync(null, 1000, 0).ToListAsync();

        CollectionAssert.AreEqual(expected, actingAsClientVars.Select(x => x.Id).ToArray());
        CollectionAssert.IsEmpty(plainClientVars);
    }

    [Test]
    public async Task CreatePolicyTest()
    {
        const string policyResponseText = "{\"created_roles\":{},\"version\":10}";
        const string policyId = "vaultname/policyname";
        var policyPath = $"{BaseUri}policies/{Client.AccountName}/{Constants.KIND_POLICY}";

        // notice: We must encode policyId, 
        Mocker.Mock(new Uri($"{policyPath}/{Uri.EscapeDataString(policyId)}"), policyResponseText);

        Client.Authenticator = new MockAuthenticator();

        await using (var policyStream = CreatePolicyStream())
        {
            var responseStream = Client.Policy(policyId).LoadPolicy(policyStream);
            AssertPolicyResponse(responseStream);
        }

        await using (var policyStream = CreatePolicyStream())
        {
            var responseStream = await Client.Policy(policyId).LoadPolicyAsync(policyStream);
            AssertPolicyResponse(responseStream);
        }

        Stream CreatePolicyStream()
        {
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, leaveOpen: true);
            sw.WriteLine("- !variable");
            sw.WriteLine("  id: TestVariable");
            sw.Flush();
            return ms;
        }

        void AssertPolicyResponse(Stream responseStream)
        {
            try
            {
                using var reader = new StreamReader(responseStream);
                Assert.AreEqual(policyResponseText, reader.ReadToEnd());
            }
            catch(Exception ex)
            {
                Assert.Fail("Failure in policy load response: " + ex);
            }
        }
    }

    [TestCase("https://example.com//test/of/test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test////of//test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test/", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com//////test////of/////test//", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test?q=p//s", ExpectedResult = "https://example.com/test/of/test/?q=p//s")]
    [TestCase("https://example.com/test/of/test?q=p%2F%2Fs", ExpectedResult = "https://example.com/test/of/test/?q=p%2F%2Fs")]
    public string TestNormalizeBaseUri(string uri) => Client.NormalizeBaseUri(uri).ToString();
}
