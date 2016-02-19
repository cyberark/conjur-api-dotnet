using NUnit.Framework;
using System;
using System.Net;

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
            Assert.AreEqual("api-key", client.LogIn("admin", "secret"));
        }
    }
}
