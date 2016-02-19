using NUnit.Framework;
using System;
using System.Net;
using Conjur;

namespace ConjurTest
{
    [TestFixture]
    public class ClientTest
    {
        private readonly Conjur.Client client;
        private readonly WebMocker mocker = new WebMocker();

        public ClientTest()
        {
            WebRequest.RegisterPrefix("test", mocker);
            client = new Conjur.Client("test:///");
        }

        [Test]
        public void TestInfo()
        {
            mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\" }");
            Assert.AreEqual("test-account", client.GetAccountName());
        }

        [Test]
        public void TestLogin()
        {
            mocker.Mock(new Uri("test:///authn/users/login"), "api-key").Verifier = 
                (WebRequest wr) =>
            {
                var cred = wr.Credentials.GetCredential(wr.RequestUri, "basic");
                Assert.AreEqual("admin", cred.UserName);
                Assert.AreEqual("secret", cred.Password);
            };

            var apiKey = client.LogIn("admin", "secret");
            Assert.AreEqual("api-key", apiKey);
            VerifyAuthenticator(client.Authenticator);
            var testRequest = client.AuthenticatedRequest("info");
            Assert.AreEqual("Token token=\"dG9rZW4=\"", // "token" base64ed
                testRequest.Headers["Authorization"]);
        }

        private void VerifyAuthenticator(IAuthenticator authenticator)
        {
            mocker.Mock(new Uri("test:///authn/users/admin/authenticate"), "token")
                .Verifier = (WebRequest wr) =>
            {
                var req = wr as WebMocker.MockRequest;
                Assert.AreEqual("POST", wr.Method);
                Assert.AreEqual("api-key", req.Body);
            };
            Assert.AreEqual("token", authenticator.GetToken());
        }
    }
}
