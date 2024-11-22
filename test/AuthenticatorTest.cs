﻿using System;
using System.Net;
using NUnit.Framework;
using System.Threading;
using System.Net.Http;

namespace Conjur.Test
{
    public class AuthenticatorTest : Base
    {
        // file deepcode ignore NoHardcodedCredentials/test: This is a test file
        protected static readonly NetworkCredential credential = new NetworkCredential("username", "api-key");

        protected ApiKeyAuthenticator Authenticator;

        [SetUp]
        public void CreateAuthenticator()
        {
            Authenticator = new ApiKeyAuthenticator(new Uri("test:///authn"), TestAccount, credential, this.Client.httpClient);
        }

        [Test]
        public void TestTokenCaching()
        {
            MockToken("token1");
            Assert.AreEqual("token1", Authenticator.GetToken());
            MockToken("token2");

            Assert.AreEqual("token1", Authenticator.GetToken());
            MockTokenExpiration();
            Assert.AreEqual("token2", Authenticator.GetToken());
        }

        [Test]
        public void TestTokenThreadSafe()
        {
            int authenticationCount = 0;
            Action<HttpRequestMessage> verifier = (HttpRequestMessage requestMessage) =>
            {
                ApiKeyVerifier(requestMessage);
                Thread.Sleep(10);
                Interlocked.Increment(ref authenticationCount);
            };

            string token = "token1";

            MockToken(token).Verifier = verifier;

            Assert.AreEqual(token, Authenticator.GetToken());
            Assert.AreEqual(1, authenticationCount);

            ThreadStart checker = () =>
            {
                Assert.AreEqual(token, Authenticator.GetToken());
            };

            Thread t1 = new Thread(checker);
            Thread t2 = new Thread(checker);

            t1.Start(); t2.Start();
            t1.Join(); t2.Join();

            Assert.AreEqual(1, authenticationCount);

            MockTokenExpiration();

            token = "token2";
            MockToken(token).Verifier = verifier;

            t1 = new Thread(checker);
            t2 = new Thread(checker);

            Assert.AreEqual(1, authenticationCount);
            t1.Start(); t2.Start();
            t1.Join(); t2.Join();
            Assert.AreEqual(2, authenticationCount);
        }

        static protected readonly Action<HttpRequestMessage> ApiKeyVerifier = (HttpRequestMessage requestMessage) =>
        {
            Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
            Assert.AreEqual("api-key", requestMessage.Content.ReadAsStringAsync().Result);
        };


        protected WebMocker.MockResponse MockToken(string token)
        {
            var mock = Mocker.Mock(new Uri($"test:///authn/{TestAccount}/username/authenticate"), token);
            mock.Verifier = ApiKeyVerifier;
            return mock;
        }

        protected void MockTokenExpiration()
        {
            Authenticator.StartTokenTimer(new TimeSpan(0, 0, 0, 0, 1));
            Thread.Sleep(10);
        }
    }
}
