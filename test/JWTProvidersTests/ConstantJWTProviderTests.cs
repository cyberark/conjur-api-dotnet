using Conjur.JWTProviders;

namespace Conjur.Test.JWTProvidersTests;

[TestFixture]
public class ConstantJWTProviderTests
{
    private const string ExpectedJWT = "test token";
    private readonly ConstantJWTProvider sut = new(ExpectedJWT);

    [Test]
    public void TestGetJWT()
    {
        var actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedJWT, actual);
        
        actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedJWT, actual);
    }

    [Test]
    public async Task TestGetJWTAsync()
    {
        var actual = await sut.GetJWTAsync(null);

        Assert.AreEqual(ExpectedJWT, actual);
        
        actual = await sut.GetJWTAsync(null);

        Assert.AreEqual(ExpectedJWT, actual);
    }
}
