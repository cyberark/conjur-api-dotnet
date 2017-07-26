using System;
using NUnit.Framework;

namespace Conjur.Test
{
    public class VariablesTest : Base
    {
        public VariablesTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestValue()
        {
            Mocker.Mock(new Uri("test:///secrets/" + TestAccount +  "/variable/foo%2Fbar"), "testvalue");
            Assert.AreEqual("testvalue", Client.Variable("foo/bar").GetValue());

            // TODO: not sure if this is supposed to be a plus or %20 or either
            Mocker.Mock(new Uri("test:///secrets/" + TestAccount +  "/variable/foo+bar"), "space test");
            Assert.AreEqual("space test", Client.Variable("foo bar").GetValue());
        }
    }
}
