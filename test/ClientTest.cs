using NUnit.Framework;
using System;
using Conjur;
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
            client = new Conjur.Client("test://");
        }

        [Test]
        public void TestInfo()
        {
            mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\" }");
            Assert.AreEqual(client.GetAccountName(), "test-account");
        }
    }
}
