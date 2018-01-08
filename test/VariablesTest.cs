using System;
using System.Net;
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

            Mocker.Mock (new Uri("test:///variables/foo%20bar/value"), "space test");
            Assert.AreEqual("space test", Client.Variable("foo bar").GetValue());
        }

        [Test]
        public void TestAddValue()
        {
            string testValue = "testvalue";

            // AddValue doesn't have a content in the response so we don't mock it
            Mocker.Mock(new Uri("test:///variables/foo%2Fbar/values"), "")
            .Verifier = (WebRequest wr) =>
            {
                var req = wr as WebMocker.MockRequest;
                Assert.AreEqual("POST", wr.Method);
                Assert.AreEqual("application/json", wr.ContentType);
                Assert.AreEqual($"{{\"value\": \"{testValue}\"}}", req.Body);
            };

            Client.Variable("foo/bar").AddValue(testValue);
        }
    }
}
