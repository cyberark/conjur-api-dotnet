using Conjur.JWTProviders;

namespace Conjur.Test;

public class JWTAuthenticatorTest : Base
{
    private const string ServiceId = "sid";
    private const string HostId = "hid";
    private readonly ConstantJWTProvider jwtProvider = new("this is a nice provider");
    private JWTAuthenticator authenticator;

    [SetUp]
    public void CreateAuthenticator()
    {
        authenticator = new JWTAuthenticator(Client, jwtProvider, ServiceId, HostId);
    }

    [Test]
    public async Task TestWithoutHostId()
    {
        authenticator = new JWTAuthenticator(Client, jwtProvider, ServiceId);

        MockToken("token1", null);
        Assert.AreEqual("token1", authenticator.GetToken());

        await MockTokenExpirationAsync();

        MockAsyncToken("token2", null);
        Assert.AreEqual("token2", await authenticator.GetTokenAsync());
    }

    [Test]
    public void TestTokenCaching()
    {
        // Mock that CyberArk returns token1
        MockToken("token1");
        Assert.AreEqual("token1", authenticator.GetToken());

        // Mock that CyberArk returns token2, but the authenticator
        // returns the valid cached token1, practically we test that
        // we don't call CyberArk again
        MockToken("token2");
        Assert.AreEqual("token1", authenticator.GetToken());

        // Expire the cached token, token1, and verify that the authenticator
        // fetches the new token2
        MockTokenExpiration();
        Assert.AreEqual("token2", authenticator.GetToken());
    }

    [Test]
    public async Task TestTokenCachingAsync()
    {
        // Mock that CyberArk returns token1
        MockAsyncToken("token1");
        Assert.AreEqual("token1", await authenticator.GetTokenAsync());

        // Mock that CyberArk returns token2, but the authenticator
        // returns the valid cached token1, practically we test that
        // we don't call CyberArk again
        MockAsyncToken("token2");
        Assert.AreEqual("token1", await authenticator.GetTokenAsync());

        // Expire the cached token, token1, and verify that the authenticator
        // fetches the new token2
        await MockTokenExpirationAsync();
        Assert.AreEqual("token2", await authenticator.GetTokenAsync());
    }

    [Test]
    public void TestTokenThreadSafe()
    {
        // this method is the same as TestTokenCaching, but we test
        // the thread safety of the GetToken method, calling the GetToken
        // method from multiple threads at the same time
        var authenticationCount = 0;
        const string token1 = "token1";
        const string token2 = "token2";

        // Mock that CyberArk returns token1
        // We call GetToken so that the token is cached
        MockToken(token1).Verifier = Verifier;
        Assert.AreEqual(token1, authenticator.GetToken());
        Assert.AreEqual(1, authenticationCount);

        // Start two threads that call GetToken at the same time
        // We expect that only one request is sent to CyberArk
        StartTwoThreads(token1, expectedAuthenticationCount: 1);

        // Expire the cached token, token1, and verify that the authenticator
        // fetches the new token2 only once from the two threads
        // causing authenticationCount to be incremented
        MockTokenExpiration();
        StartTwoThreads(token2, expectedAuthenticationCount: 2);

        void StartTwoThreads(string expectedToken, int expectedAuthenticationCount)
        {
            MockToken(expectedToken).Verifier = Verifier;
            var startSignal = new ManualResetEvent(false);
            var t1 = new Thread(() => Checker(expectedToken, startSignal));
            var t2 = new Thread(() => Checker(expectedToken, startSignal));

            t1.Start(); t2.Start();

            // Release both threads simultaneously
            startSignal.Set();

            t1.Join(); t2.Join();

            Assert.AreEqual(expectedAuthenticationCount, authenticationCount);
        }

        void Verifier(HttpRequestMessage requestMessage)
        {
            JwtContentVerifier(requestMessage);
            Thread.Sleep(10);
            Interlocked.Increment(ref authenticationCount);
        }

        void Checker(string expected, ManualResetEvent signal)
        {
            signal.WaitOne();
            Assert.AreEqual(expected, authenticator.GetToken());
        }
    }

    [Test]
    public async Task TestTokenThreadSafeAsync()
    {
        // this method is the same as TestTokenCachingAsync, but we test
        // the thread safety of the GetToken method, calling the GetToken
        // method from multiple threads at the same time
        var authenticationCount = 0;
        var token = "token1";

        // Mock that CyberArk returns token1
        // We call GetToken so that the token is cached
        MockAsyncToken(token).VerifierAsync = VerifierAsync;
        Assert.AreEqual(token, await authenticator.GetTokenAsync());
        Assert.AreEqual(1, authenticationCount);

        // Start multiple tasks that call GetTokenAsync at the same time
        var tasks = CreateTasks(token);

        // We change the mock to a fake token, to ensure that the authenticator
        // returns the cached token1, and doesn't call CyberArk again
        MockAsyncToken("fake").VerifierAsync = VerifierAsync;

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
        // We should have called CyberArk only once
        Assert.AreEqual(1, authenticationCount);

        // We expire the token, and set the mock to return token2
        await MockTokenExpirationAsync();
        token = "token2";
        MockAsyncToken(token).VerifierAsync = VerifierAsync;

        tasks = CreateTasks(token);

        await Task.WhenAll(tasks);
        // We should have called CyberArk only once, now authenticationCount is 2
        Assert.AreEqual(2, authenticationCount);

        // We expire the token, and set the mock to return token3
        await MockTokenExpirationAsync();
        token = "token3";
        MockAsyncToken(token).VerifierAsync = VerifierAsync;

        // Start multiple tasks that call GetTokenAsync at the same time
        // to increase the chance of contention
        tasks = [.. Enumerable.Range(0, 20).Select(_ => Checker(token))];

        await Task.WhenAll(tasks);
        // We should have called CyberArk only once, now authenticationCount is 3
        Assert.AreEqual(3, authenticationCount);

        async Task VerifierAsync(HttpRequestMessage requestMessage)
        {
            await JwtContentVerifierAsync(requestMessage);
            await Task.Delay(50);
            Interlocked.Increment(ref authenticationCount);
        }

        async Task<string> Checker(string expected)
        {
            var actual = await authenticator.GetTokenAsync();
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
                var actual = authenticator.GetToken();
                Assert.AreEqual(expected, actual);
                return actual;
            }),
            Checker(expected),
        ];
    }

    private static void JwtContentVerifier(HttpRequestMessage requestMessage)
    {
        Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
        Assert.AreEqual("jwt=this+is+a+nice+provider", requestMessage.Content!.ReadAsStringAsync().Result);
    }

    private static async Task JwtContentVerifierAsync(HttpRequestMessage requestMessage)
    {
        Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
        Assert.AreEqual("jwt=this+is+a+nice+provider", await requestMessage.Content!.ReadAsStringAsync());
    }

    private WebMocker.MockResponse MockToken(string token, string hostId = HostId)
    {
        hostId = string.IsNullOrEmpty(hostId) ? hostId : hostId + "/";
        var mock = Mocker.Mock(new Uri($"test://example.com/authn-jwt/{ServiceId}/{TestAccount}/{hostId}authenticate"), token);
        mock.Verifier = JwtContentVerifier;
        return mock;
    }

    private WebMocker.MockResponse MockAsyncToken(string token, string hostId = HostId)
    {
        var mock = MockToken(token, hostId);
        mock.VerifierAsync = JwtContentVerifierAsync;
        return mock;
    }

    protected void MockTokenExpiration()
    {
        authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
        Thread.Sleep(20);
        Thread.Yield();
    }

    protected async Task MockTokenExpirationAsync()
    {
        authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
        await Task.Delay(20);
    }
}
