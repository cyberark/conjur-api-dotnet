using NUnit.Framework;
using System;
using System.Net;
using Conjur;

namespace Conjur.Test
{
    public class ClientTest : Base
    {
        [Test]
        public void TestInfo()
        {
            Assert.AreEqual("test-account", Client.GetAccountName());
        }

        [Test]
        public void TestLogin()
        {
            Mocker.Mock(new Uri("test:///authn/users/login"), "api-key").Verifier = 
                (WebRequest wr) =>
            Assert.AreEqual("Basic YWRtaW46c2VjcmV0", wr.Headers["Authorization"]);

            var apiKey = Client.LogIn("admin", "secret");
            Assert.AreEqual("api-key", apiKey);
            VerifyAuthenticator(Client.Authenticator);
        }

        private void VerifyAuthenticator(IAuthenticator authenticator)
        {
            Mocker.Mock(new Uri("test:///authn/users/admin/authenticate"), "token")
                .Verifier = (WebRequest wr) =>
            {
                var req = wr as WebMocker.MockRequest;
                Assert.AreEqual("POST", wr.Method);
                Assert.AreEqual("api-key", req.Body);
            };
            Assert.AreEqual("token", authenticator.GetToken());
        }

        [Test]
        public void TestAuthenticatedRequest()
        {
            Mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\" }");
            Client.Authenticator = new MockAuthenticator();
            var testRequest = Client.AuthenticatedRequest("info");
            Assert.AreEqual("Token token=\"dG9rZW4=\"", // "token" base64ed
                testRequest.Headers["Authorization"]);

            Client.Authenticator = null;
            Assert.Throws<InvalidOperationException>(() => 
                Client.AuthenticatedRequest("info"));
        }
    }
}
