using System;
using System.Net;
using NUnit.Framework;

namespace Conjur.Test
{
    public class ResourceTest : Base
    {
<<<<<<< HEAD
        protected readonly ResourceKind Kind = ResourceKind.user;
=======
        protected readonly string Kind = Constants.KIND_USER;
>>>>>>> 9b1fc3a577d209f9dc2470af980fdd7e44a95d22
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