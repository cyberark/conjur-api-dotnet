﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

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
                (HttpRequestMessage requestMessage) =>
            Assert.AreEqual("Basic YWRtaW46c2VjcmV0", requestMessage.Headers.GetValues("Authorization").SingleOrDefault());

            var apiKey = Client.LogIn("admin", "secret");
            Assert.AreEqual("api-key", apiKey);
            VerifyAuthenticator(Client.Authenticator);
        }

        private void VerifyAuthenticator(IAuthenticator authenticator)
        {
            Mocker.Mock(new Uri("test:///authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token")
                .Verifier = (HttpRequestMessage requestMessage) =>
                {
                    Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
                    Assert.AreEqual("api-key", requestMessage.Content.ReadAsStringAsync().Result);
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
                testRequest.Headers.GetValues("Authorization").Single());

            Client.Authenticator = null;
            Assert.Throws<InvalidOperationException>(() =>
                Client.AuthenticatedRequest("info"));
        }

        [Test]
        public void ActingAsTest()
        {
            // Test in mono fails when using : in role variable. role should be TestAccount:Kind:foo
            string role = "foo";
            string resourceVarUri = $"test:///resources/{TestAccount}/{Constants.KIND_VARIABLE}";

            Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000&acting_as={role}"), $"[{{\"id\":\"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id\"}}]");
            Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000"), "[]");

            Client.Authenticator = new MockAuthenticator();

            IEnumerator<Variable> actingAsClientVars = Client.ActingAs(role).ListVariables(null, 1000, 0).GetEnumerator();
            IEnumerator<Variable> plainClientVars = Client.ListVariables(null, 1000, 0).GetEnumerator();

            Assert.AreEqual(true, actingAsClientVars.MoveNext());
            Assert.AreEqual($"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id", actingAsClientVars.Current.Id);

            Assert.AreEqual(false, plainClientVars.MoveNext());
        }

        [Test]
        public void CreatePolicyTest()
        {
            string policyId = "vaultname/policyname";
            string policyPath = $"test:///policies/{Client.GetAccountName()}/{Constants.KIND_POLICY}";
            string policyResponseText = "{\"created_roles\":{},\"version\":10}";

            // notice: We must encode policyId, 
            Mocker.Mock(new Uri($"{policyPath}/{Uri.EscapeDataString(policyId)}"), policyResponseText);

            Client.Authenticator = new MockAuthenticator();

            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms))
                {
                    sw.WriteLine("- !variable");
                    sw.WriteLine("  id: TestVariable");

                    Stream policyResStream = Client.Policy(policyId).LoadPolicy(ms);

                    try
                    {
                        StreamReader reader = new StreamReader(policyResStream);
                        Assert.AreEqual(policyResponseText, reader.ReadToEnd());
                    }
                    catch
                    {
                        Assert.Fail("Failure in policy load response");
                    }
                }
            }
        }
    }
}