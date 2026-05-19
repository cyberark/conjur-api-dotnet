namespace Conjur.Test;

public class CertAuthenticatorTest : Base
{
    protected CertAuthenticator Authenticator;

    [SetUp]
    public void CreateAuthenticator()
    {
        Authenticator = new CertAuthenticator(Client, "my-service", "host/vm-workloads/vm-01");
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
        var authenticationCount = 0;
        var token = "token1";

        MockToken(token).Verifier = Verifier;

        Assert.AreEqual(token, Authenticator.GetToken());
        Assert.AreEqual(1, authenticationCount);

        var t1 = new Thread(Checker);
        var t2 = new Thread(Checker);

        t1.Start(token);
        t2.Start(token);
        t1.Join();
        t2.Join();

        Assert.AreEqual(1, authenticationCount);

        MockTokenExpiration();

        token = "token2";
        MockToken(token).Verifier = Verifier;

        t1 = new Thread(Checker);
        t2 = new Thread(Checker);

        Assert.AreEqual(1, authenticationCount);
        t1.Start(token);
        t2.Start(token);
        t1.Join();
        t2.Join();
        Assert.AreEqual(2, authenticationCount);

        void Verifier(HttpRequestMessage requestMessage)
        {
            RequestVerifier(requestMessage);
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
        var authenticationCount = 0;
        var token = "token1";

        MockToken(token).VerifierAsync = VerifierAsync;

        Assert.AreEqual(token, await Authenticator.GetTokenAsync());
        Assert.AreEqual(1, authenticationCount);

        var tasks = CreateTasks(token);

        MockToken("fake").VerifierAsync = VerifierAsync;

        await Task.WhenAll(tasks);

        Assert.AreEqual(1, authenticationCount);

        MockTokenExpiration();

        token = "token2";
        MockToken(token).VerifierAsync = VerifierAsync;

        tasks = CreateTasks(token);

        await Task.WhenAll(tasks);
        Assert.AreEqual(2, authenticationCount);

        MockTokenExpiration();

        token = "token3";
        MockToken(token).VerifierAsync = VerifierAsync;

        tasks = [.. Enumerable.Range(0, 20).Select(_ => Checker(token))];

        await Task.WhenAll(tasks);
        Assert.AreEqual(3, authenticationCount);

        async Task VerifierAsync(HttpRequestMessage requestMessage)
        {
            RequestVerifier(requestMessage);
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

    [Test]
    public void TestSpiffeMode()
    {
        // Test SPIFFE mode with empty host ID
        var spiffeAuthenticator = new CertAuthenticator(Client, "my-service", "");
        MockTokenSpiffe("spiffe-token");
        Assert.AreEqual("spiffe-token", spiffeAuthenticator.GetToken());
    }

    protected static readonly Action<HttpRequestMessage> RequestVerifier = requestMessage =>
    {
        Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
    };

    protected WebMocker.MockResponse MockToken(string token)
    {
        var mock = Mocker.Mock(
            new Uri($"{BaseUri}authn-cert/my-service/{TestAccount}/host%2Fvm-workloads%2Fvm-01/authenticate"),
            token);
        mock.Verifier = RequestVerifier;
        return mock;
    }

    protected WebMocker.MockResponse MockTokenSpiffe(string token)
    {
        var mock = Mocker.Mock(
            new Uri($"{BaseUri}authn-cert/my-service/{TestAccount}/authenticate"),
            token);
        mock.Verifier = RequestVerifier;
        return mock;
    }

    protected void MockTokenExpiration()
    {
        Authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
        Thread.Sleep(20);
        Thread.Yield();
    }
}


