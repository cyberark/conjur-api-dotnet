using Conjur.JWTProviders;

namespace Conjur.Test.JWTProvidersTests;

[TestFixture]
public class InterfaceJWTProviderTests
{
    private const string ExpectedJWT = "test token";
    private readonly IJWTProvider sut = new DummyProvider(ExpectedJWT);

    [Test]
    public void TestGetJWT()
    {
        var actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedJWT, actual);
        
        actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedJWT, actual);
    }

    private class DummyProvider(string value) : IJWTProvider
    {
        public Task<string> GetJWTAsync(object data, CancellationToken cancellationToken = default) => Task.FromResult(value);
    }
}
