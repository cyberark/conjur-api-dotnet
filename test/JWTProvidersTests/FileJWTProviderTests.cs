using Conjur.JWTProviders;

namespace Conjur.Test.JWTProvidersTests;

[TestFixture]
public class FileJWTProviderTests
{
    private const string ExpectedFileContent = "test token";
    private readonly string filePath = Path.Combine("JWTProvidersTests", "jwt.token");

    [Test]
    public void TestGetJWT()
    {
        var sut = new FileJWTProvider(filePath);

        var actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedFileContent, actual);
        
        actual = sut.GetJWT(null);

        Assert.AreEqual(ExpectedFileContent, actual);
    }

    [Test]
    public async Task TestGetJWTAsync()
    {
        var sut = new FileJWTProvider(filePath);

        var actual = await sut.GetJWTAsync(null);

        Assert.AreEqual(ExpectedFileContent, actual);
        
        actual = await sut.GetJWTAsync(null);

        Assert.AreEqual(ExpectedFileContent, actual);
    }
}