namespace Conjur.Test;

public class AuthenticatorTest : Base
{
    // file deepcode ignore NoHardcodedCredentials/test: This is a test file
    protected static readonly NetworkCredential Credential = new("username", "api-key");

    protected ApiKeyAuthenticator Authenticator;

    [SetUp]
    public void CreateAuthenticator()
    {
        Authenticator = new ApiKeyAuthenticator(new Uri("test:///authn"), TestAccount, Credential, Client);
    }

    [Test]
    public void TestTokenCaching()
    {
        MockToken("token1");
        Assert.AreEqual("token1", Authenticator.GetToken());
        MockToken("token2");

        Assert.AreEqual("token1", Authenticator.GetToken());
        MockTokenExpiration();
        Assert.AreEqual("token2", Authenticator.GetToken());
    }

    [Test]
    public async Task TestTokenCachingAsync()
    {
        MockToken("token1");
        Assert.AreEqual("token1", await Authenticator.GetTokenAsync());
        MockToken("token2");

        Assert.AreEqual("token1", await Authenticator.GetTokenAsync());
        MockTokenExpiration();
        Assert.AreEqual("token2", await Authenticator.GetTokenAsync());
    }

    [Test]
    public void TestTokenThreadSafe()
    {
        // This method is testing that calling ApiKeyAuthenticator.GetToken is thread safe, the token
        // is correctly cached until it expired.
        // First part is testing that we return the same token when called from two threads.
        // The second part is testing that we return the same token when called from two threads after token expired.
        var authenticationCount = 0;
        var token = "token1";

        // We set the mock to return the token and set the request verifier to `Verifier` method.
        MockToken(token).Verifier = Verifier;

        // Call and assert that we get the expected token, and we only performed one request to CyberArk Conjur.
        Assert.AreEqual(token, Authenticator.GetToken());
        Assert.AreEqual(1, authenticationCount);

        // We create two threads that will run in parallel to call GetToken
        var t1 = new Thread(Checker);
        var t2 = new Thread(Checker);

        t1.Start(token); t2.Start(token);
        t1.Join(); t2.Join();

        Assert.AreEqual(1, authenticationCount);

        // Set the cache token to expire.
        MockTokenExpiration();

        // Same as before but we test that the new token is fetched only once
        // after the old one expired.
        token = "token2";
        MockToken(token).Verifier = Verifier;

        t1 = new Thread(Checker);
        t2 = new Thread(Checker);

        Assert.AreEqual(1, authenticationCount);
        t1.Start(token); t2.Start(token);
        t1.Join(); t2.Join();
        Assert.AreEqual(2, authenticationCount);

        void Verifier(HttpRequestMessage requestMessage)
        {
            ApiKeyVerifier(requestMessage);
            Thread.Sleep(10);
            Interlocked.Increment(ref authenticationCount);
        }

        void Checker(object expected)
        {
            Assert.AreEqual(expected, Authenticator.GetToken());
        }
    }

    [Test]
    public async Task TestTokenThreadSafeAsync()
    {
        // This method is testing that calling ApiKeyAuthenticator.GetTokenAsync is thread safe, the token
        // is correctly cached until it expired.
        // First part is testing that we return the same token when called from different threads.
        // The second part is testing that we return the same token when called from two threads after token expired.
        var authenticationCount = 0;
        var token = "token1";

        // Set the mock to return token and set request verifier to be `VerifierAsync`
        MockToken(token).VerifierAsync = VerifierAsync;
        
        // Call and assert that we get the expected token, and we only performed one request to CyberArk Conjur.
        Assert.AreEqual(token, await Authenticator.GetTokenAsync());
        Assert.AreEqual(1, authenticationCount);

        // We create multiple tasks that will run in parallel to call GetTokenAsync
        var tasks = CreateTasks(token);

        // We change the mock to return a different token to check that the cached token is returned
        MockToken("fake").VerifierAsync = VerifierAsync;

        await Task.WhenAll(tasks);

        // Assert that the only one request was done to CyberArk Conjur
        Assert.AreEqual(1, authenticationCount);

        // Set the cache token to expire.
        MockTokenExpiration();

        // Same as before but we test that the new token is fetched only once
        // after the old one expired.
        token = "token2";
        MockToken(token).VerifierAsync = VerifierAsync;

        tasks = CreateTasks(token);

        await Task.WhenAll(tasks);
        Assert.AreEqual(2, authenticationCount);

        MockTokenExpiration();

        // Same as previous, but with more tasks to increase the chance of collision.
        token = "token3";
        MockToken(token).VerifierAsync = VerifierAsync;

        tasks = [.. Enumerable.Range(0, 20).Select(_ => Checker(token))];

        await Task.WhenAll(tasks);
        Assert.AreEqual(3, authenticationCount);

        async Task VerifierAsync(HttpRequestMessage requestMessage)
        {
            ApiKeyVerifier(requestMessage);
            await Task.Delay(50);
            Interlocked.Increment(ref authenticationCount);
        }

        async Task<string> Checker(string expected)
        {
            var actual = await Authenticator.GetTokenAsync();
            Assert.AreEqual(expected, actual);
            return actual;
        }
        Task<string>[] CreateTasks(string expected) =>
        [
            Checker(expected),
            Checker(expected),
            Checker(expected),
            Task.Run(() =>
            {
                var actual = Authenticator.GetToken();
                Assert.AreEqual(expected, actual);
                return actual;
            }),
            Checker(expected),
        ];
    }

    protected static readonly Action<HttpRequestMessage> ApiKeyVerifier = requestMessage =>
    {
        Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
        Assert.AreEqual("api-key", requestMessage.Content!.ReadAsStringAsync().Result);
    };


    protected WebMocker.MockResponse MockToken(string token)
    {
        var mock = Mocker.Mock(new Uri($"test:///authn/{TestAccount}/username/authenticate"), token);
        mock.Verifier = ApiKeyVerifier;
        return mock;
    }

    protected void MockTokenExpiration()
    {
        Authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
        Thread.Sleep(20);
        Thread.Yield();
    }
}
