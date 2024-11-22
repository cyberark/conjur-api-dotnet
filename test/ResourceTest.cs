using System;
using System.Net;
using NUnit.Framework;

namespace Conjur.Test
{
    public class ResourceTest : Base
    {
        protected readonly string Kind = Constants.KIND_USER;
        protected readonly string Name = "bacon";

        public ResourceTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestCheck()
        {
            var resource = Client.Resource(Kind, Name);

            Mocker.Mock(new Uri("test:///resources/" + TestAccount
                + "/" + Kind + "/" + Name + "/?check=true&privilege=fry"), "");
            Assert.IsTrue(resource.Check("fry"));

            Mocker.Mock(new Uri("test:///resources/" + TestAccount
                    + "/" + Kind + "/" + Name + "/?check=true&privilege=fry"), "", HttpStatusCode.Forbidden);
            Assert.IsFalse(resource.Check("fry"));
        }

        [Test]
        public void TestNameToId()
        {
            Resource resource = Client.Resource(Kind, Name);
            Assert.AreEqual($"{Client.GetAccountName()}:{Kind}:{Name}", resource.Id);
        }
    }
}
