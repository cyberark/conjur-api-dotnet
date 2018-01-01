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
        public void TestGetValue()
        {
            Mocker.Mock(new Uri("test:///variables/foo%2Fbar/value"), "testvalue");
            Assert.AreEqual("testvalue", Client.Variable("foo/bar").GetValue());

            // TODO: not sure if this is supposed to be a plus or %20 or either
            Mocker.Mock(new Uri("test:///variables/foo%20bar/value"), "space test");
            Assert.AreEqual("space test", Client.Variable("foo bar").GetValue());
        }

        [Test]
        public void TestAddValue()
        {
            Mocker.Mock (new Uri ("test:///variables/foo%2Fbar/values"), "testvalue");
            Client.Variable ("foo/bar").AddValue ("testvalue");

            string res = Mocker.Mock (new Uri ("test:///variables/foo%2Fbar/value"), "testvalue").Read ();
            Assert.AreEqual (res, "testvalue");
        }
    }
}
