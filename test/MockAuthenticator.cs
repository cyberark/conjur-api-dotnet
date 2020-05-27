namespace Conjur.Test
{
    public class MockAuthenticator : IAuthenticator
    {
        public string GetToken()
        {
            return "token";
        }
    }
}
