using System;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

            Mocker.Mock(new Uri("test:///variables/foo%20bar/value"), "space test");
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

        [Test]
        public void TestListVariables()
        {
            String varListUrl = "test:///authz/test-account/resources/variable";
            IEnumerator<Variable> vars;

            // Verify REST requests and response handling as expected. 
            // Verify offset, and limit evaluation made as expected
            ClearMocker();
            Mocker.Mock(new Uri($"{varListUrl}?offset=0&limit=1000"), GenerateVariablesInfo(0, 1000));
            Mocker.Mock(new Uri($"{varListUrl}?offset=1000&limit=1000"), GenerateVariablesInfo(1000, 2000));
            Mocker.Mock(new Uri($"{varListUrl}?offset=2000&limit=1000"), "[]");
            vars = Client.ListVariables().GetEnumerator();
            verifyVariablesInfo(vars, 2000);

            // Verify parameters of GetListVariables() passed as expected toward conjur server
            ClearMocker();
            Client.ActingAs = "user:role";
            Mocker.Mock(new Uri($"{varListUrl}?offset=0&limit=1000&search=var_0&acting_as=user:role"), GenerateVariablesInfo(0, 1000));
            Mocker.Mock(new Uri($"{varListUrl}?offset=1000&limit=1000&search=var_0&acting_as=user:role"), GenerateVariablesInfo(1000, 1872));
            Mocker.Mock(new Uri($"{varListUrl}?offset=1872&limit=1000&search=var_0&acting_as=user:role"), "[]");
            vars = (Client.ListVariables("var_0")).GetEnumerator();
            verifyVariablesInfo(vars, 1872);

            // Check handling invalid json response from conjur server
            ClearMocker();
            Mocker.Mock(new Uri($"{varListUrl}?offset=0&limit=1000&acting_as=user:role"), @"[""id"":""ivnalidjson""]");
            vars = (Client.ListVariables()).GetEnumerator();
            Assert.Throws<SerializationException>(() => vars.MoveNext());
        }

        private void verifyVariablesInfo(IEnumerator<Variable> vars, int expectedNumVars)
        {
            for (int id = 0; id < expectedNumVars; ++id)
            {
                Assert.AreEqual(true, vars.MoveNext());
                Assert.AreEqual($"id{id}", vars.Current.Id);
            }
            Assert.AreEqual(false, vars.MoveNext());
        }

        private string GenerateVariablesInfo(int firstVarId, int lastVarId)
        {
            Random rnd = new Random();
            string res = "";

            for (int varId = firstVarId; varId < lastVarId; varId++)
            {
                string permissions = "";
                string annoations = "";
                int nPermissions = rnd.Next(4);
                int nAnnoations = rnd.Next(4);

                for (int perId = 0; perId < nPermissions; perId++)
                {
                    permissions += (perId == 0) ? "" : ",";
                    permissions += $"{{\"privilege\":\"privilege{varId}_{perId}\"," +
                              $"\"grant_option\":\"grant_option{varId}_{perId}\"," +
                              $"\"resource\":\"resource{varId}_{perId}\"," +
                              $"\"role\":\"role{varId}_{perId}\"," +
                              $"\"grantor\":\"grantor{varId}_{perId}\" }}";
                }
                for (int annotId = 0; annotId < nAnnoations; annotId++)
                {
                    annoations += (annotId == 0) ? "" : ",";
                    annoations += $"{{\"resource_id\":\"resource_id{varId}_{annotId}\"," +
                              $"\"name\":\"name{varId}_{annotId}\"," +
                              $"\"value\":\"value{varId}_{annotId}\"," +
                              $"\"created_at\":\"created_at{varId}_{annotId}\"," +
                              $"\"updated_at\":\"updated_at{varId}_{annotId}\" }}";
                }
                res += (varId == firstVarId) ? "" : ",";
                res += $"{{\"id\":\"id{varId}\"," +
                          $"\"created_at\":\"created_at{varId}\"," +
                          $"\"owner\":\"owner{varId}\"," +
                          $"\"created_by\":\"created_by{varId}\"," +
                          $"\"permissions\":[{permissions}]," +
                          $"\"annotations\":[{annoations}]  }}";
            }
            return $"[{res}]";
        }

    }
}
