using System;
using NUnit.Framework;
using System.Net;

namespace Conjur.Test
{
    [TestFixture]
    public abstract class Base
    {
        protected readonly Client Client;
        protected readonly string TestAccount = "test-account";
        protected readonly string LoginName = "admin";
        protected static readonly WebMocker Mocker = new WebMocker();

        [SetUp]
        protected void ClearMocker()
        {
            Mocker.Clear();
            Mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\"}");
        }

        protected Base()
        {
            Client = new Client("test:///", TestAccount);
            Client.httpClient = Mocker.GetMockHttpClient();
        }
    }
}
