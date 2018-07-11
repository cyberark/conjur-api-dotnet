using System;
using System.Net;
using NUnit.Framework;

namespace Conjur.Test
{
    public class ResourceTest : Base
    {
        protected readonly string Kind = Constants.KIND_USER;
        protected readonly string Id = "bacon";

        public ResourceTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestCheck()
        {
            var resource = Client.Resource(Kind, Id);

            var mock = Mocker.Mock(new Uri("test:///resources/" + TestAccount
                   + "/" + Kind + "/" + Id + "/?check=true&privilege=fry"), "");
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
