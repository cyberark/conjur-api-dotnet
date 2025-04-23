namespace Conjur.Test;

public class MockAuthenticator : IAuthenticator
{
    public string GetToken() => "token";

    public Task<string> GetTokenAsync(CancellationToken cancellationToken) => Task.FromResult("token");
}
