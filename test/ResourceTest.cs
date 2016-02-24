using System;
using System.Net;
using NUnit.Framework;

namespace Conjur.Test
{
    public class ResourceTest : Base
    {
        public ResourceTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestCheck()
        {
            var resource = Client.Resource("chunky", "bacon");

            var mock = Mocker.Mock(new Uri("test:///authz/test-account/resources/chunky/bacon/" +
                               "?check=true&privilege=fry"), "");
            Assert.IsTrue(resource.Check("fry"));

            mock.Verifier = (WebRequest) =>
            {
                throw new WebMocker.MockResponseException(
                    HttpStatusCode.Forbidden, "Forbidden");
            };
            Assert.IsFalse(resource.Check("fry"));
        }
    }
}

