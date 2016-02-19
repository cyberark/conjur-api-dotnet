using NUnit.Framework;
using System;
using System.Net;
using System.Text;
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
            var request = WebRequest.CreateHttp("https://example.com/");
            authenticator.Apply(request);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("token"));
            Assert.AreEqual("Token token=\"" + token + "\"", 
                request.Headers["Authorization"]);
        }
    }
}
