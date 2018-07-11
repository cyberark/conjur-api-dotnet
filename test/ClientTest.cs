using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;

namespace Conjur.Test
{
    public class ClientTest : Base
    {
        [Test]
        public void TestInfo()
        {
            Assert.AreEqual(TestAccount, Client.GetAccountName());
        }

        [Test]
        public void TestLogin()
        {
            Mocker.Mock(new Uri("test:///authn/" + TestAccount + "/login"), "api-key").Verifier =
                (WebRequest wr) =>
            Assert.AreEqual("Basic YWRtaW46c2VjcmV0", wr.Headers["Authorization"]);

            var apiKey = Client.LogIn("admin", "secret");
            Assert.AreEqual("api-key", apiKey);
            VerifyAuthenticator(Client.Authenticator);
        }

        private void VerifyAuthenticator(IAuthenticator authenticator)
        {
            Mocker.Mock(new Uri("test:///authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token")
                .Verifier = (WebRequest wr) =>
                {
                    var req = wr as WebMocker.MockRequest;
                    Assert.AreEqual("POST", wr.Method);
                    Assert.AreEqual("api-key", req.Body);
                };
            Assert.AreEqual("token", authenticator.GetToken());
        }

        [Test]
        public void TestAuthenticatedRequest()
        {
            Mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\" }");
            Client.Authenticator = new MockAuthenticator();
            var testRequest = Client.AuthenticatedRequest("info");
            Assert.AreEqual("Token token=\"dG9rZW4=\"", // "token" base64ed
                testRequest.Headers["Authorization"]);

            Client.Authenticator = null;
            Assert.Throws<InvalidOperationException>(() =>
                Client.AuthenticatedRequest("info"));
        }

        [Test]
        public void ActingAsTest()
        {
            string role = $"{Client.GetAccountName()}:{Constants.KIND_USER}:foo";
            string resourceVarUri = $"test:///resources/{TestAccount}?{Constants.KIND_VARIABLE}";

            Mocker.Mock(new Uri($"{resourceVarUri}&offset=0&limit=1000&acting_as={role}"), $"[{{\"id\":\"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id\"}}]");
            Mocker.Mock(new Uri ($"{resourceVarUri}&offset=0&limit=1000"), "[]");

            Client.Authenticator = new MockAuthenticator();

            IEnumerator<Variable> actingAsClientVars = Client.ActingAs(role).ListVariables().GetEnumerator();
            IEnumerator<Variable> plainClientVars = Client.ListVariables().GetEnumerator();

            Assert.AreEqual(true, actingAsClientVars.MoveNext());
            Assert.AreEqual($"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id", actingAsClientVars.Current.Id);

            Assert.AreEqual(false, plainClientVars.MoveNext());
        }
    }
}
