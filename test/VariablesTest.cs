using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using static Conjur.Test.WebMocker;

namespace Conjur.Test
{
    public class VariablesTest : Base
    {
        public VariablesTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void GetVariableTest()
        {
            Mocker.Mock(new Uri("test:///secrets/" + TestAccount + "/variable/foo%2Fbar"), "testvalue");
            Assert.AreEqual("testvalue", Client.Variable("foo/bar").GetValue());

            Mocker.Mock(new Uri("test:///secrets/" + TestAccount + "/variable/foo%20bar"), "space test");
            Assert.AreEqual("space test", Client.Variable("foo bar").GetValue());
        }

        [Test]
        public void AddSecretTest()
        {
            char[] testValue = { 't', 'e', 's', 't', 'V', 'a', 'l', 'u', 'e' };

            var v = Mocker.Mock(new Uri("test:///secrets/" + TestAccount + "/variable/foobar"), "");
            v.Verifier = request =>
            {
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.AreEqual("text/plain", request.Content.Headers.ContentType.MediaType);
                using (StreamReader sr = new StreamReader(request.Content.ReadAsStream()))
                {
                    Assert.AreEqual(testValue, sr.ReadToEnd());
                }
            };
            Client.Variable("foobar").AddSecret(Encoding.UTF8.GetBytes(testValue));
        }

        [Test]
        public void ListVariableTest()
        {
            string variableUri = $"test:///resources/{TestAccount}/{Constants.KIND_VARIABLE}";
            IEnumerator<Variable> vars;

            ClearMocker ();
            Mocker.Mock (new Uri (variableUri + "?offset=0&limit=1000"), GenerateVariablesInfo(0, 1000));
            Mocker.Mock (new Uri (variableUri + "?offset=1000&limit=1000"), "[]");
            vars = Client.ListVariables (null, 1000, 0).GetEnumerator ();
            VerifyVariablesInfo (vars, 0, 1000);

            ClearMocker ();
            Mocker.Mock (new Uri (variableUri + "?offset=1000&limit=1000"), GenerateVariablesInfo (1000, 2000));
            Mocker.Mock (new Uri (variableUri + "?offset=2000&limit=1000"), "[]");
            vars = Client.ListVariables (null, 1000, 1000).GetEnumerator ();
            VerifyVariablesInfo (vars, 1000, 2000);

            ClearMocker ();
            Mocker.Mock(new Uri(variableUri + "?offset=0&limit=1000"), @"[""id"":""invalidjson""]");
            vars = Client.ListVariables(null,1000,0).GetEnumerator();
            Assert.Throws<SerializationException>(() => vars.MoveNext());
        }

        [Test]
        public void CountVariablesTest()
        {
            string variableUri = $"test:///resources/{TestAccount}/{Constants.KIND_VARIABLE}";

            ClearMocker();
            Mocker.Mock(new Uri(variableUri + "?count=true&search=dummy"), @"{""count"":10}");

            uint result = Client.CountVariables("dummy");
            Assert.AreEqual(result, 10);
        }

        //Typo in excpectedNumVars?
        private void VerifyVariablesInfo(IEnumerator<Variable> vars, int offset, int excpectedNumVars)
        {
            for (int id = offset; id < excpectedNumVars; ++id)
            {
                Assert.AreEqual(true, vars.MoveNext());
                Assert.AreEqual($"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id{id}", vars.Current.Id);
            }
            Assert.AreEqual(false, vars.MoveNext());
        }

        private string GenerateVariablesInfo(int firstVarId, int lastVarId)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int varId = firstVarId; varId < lastVarId; varId++)
            {
                stringBuilder.Append($"{{\"id\":\"{Client.GetAccountName()}:{Constants.KIND_VARIABLE}:id{varId}\"}},");
            }
            if (stringBuilder.Length != 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            return $"[{stringBuilder}]";
        }
    }
}
