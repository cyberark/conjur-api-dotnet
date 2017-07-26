using System;
using System.Net;
using NUnit.Framework;
using System.Threading;

namespace Conjur.Test
{
    public class AuthenticatorTest : Base
    {
        [Test]
        public void TestTokenCaching()
        {
            Action<WebRequest> verifier = (WebRequest wr) =>
            {
                var req = wr as WebMocker.MockRequest;
                Assert.AreEqual("POST", wr.Method);
                Assert.AreEqual("api-key", req.Body);
            };

            Mocker.Mock(new Uri("test:///authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token1")
                .Verifier = verifier;

            var credential = new NetworkCredential(LoginName, "api-key");
            var authenticator = new ApiKeyAuthenticator(new Uri("test:///authn"), TestAccount ,credential);

            Assert.AreEqual("token1", authenticator.GetToken());

            Mocker.Mock(new Uri("test:///authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token2")
                .Verifier = verifier;

            Assert.AreEqual("token1", authenticator.GetToken());

            authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
            Thread.Sleep(10);

            Assert.AreEqual("token2", authenticator.GetToken());
        }
    }
}
