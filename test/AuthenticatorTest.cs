using System;
using System.Net;
using NUnit.Framework;
using System.Threading;
using System.Reflection;

namespace Conjur.Test
{
    public class AuthenticatorTest : Base
    {
        [Test]
        public void TestTokenCaching()
        {
            Action<WebRequest> verifier = (WebRequest wr) =>
            {
                WebMocker.MockRequest req = wr as WebMocker.MockRequest;
                Assert.AreEqual("POST", wr.Method);
                Assert.AreEqual("api-key", req.Body);
            };
            
            Mocker.Mock(new Uri("test:///authn/users/username/authenticate"), "token1")
                .Verifier = verifier;
            
            NetworkCredential credential = new NetworkCredential("username", "api-key");
            ApiKeyAuthenticator authenticator = new ApiKeyAuthenticator(new Uri("test:///authn"), credential);

            Assert.AreEqual("token1", authenticator.GetToken());

            Mocker.Mock(new Uri("test:///authn/users/username/authenticate"), "token2")
                .Verifier = verifier;

            Assert.AreEqual("token1", authenticator.GetToken());

            FieldInfo tokenField = authenticator.GetType().GetField("token", BindingFlags.NonPublic | BindingFlags.Instance);
            ApiKeyAuthenticator.Token token = new ApiKeyAuthenticator.Token("token1", new TimeSpan(0, 0, 0, 0, 1));
            tokenField.SetValue(authenticator, token);

            Thread.Sleep(10);

            Assert.AreEqual("token2", authenticator.GetToken());
        }
    }
}

