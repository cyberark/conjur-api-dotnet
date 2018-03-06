using System;
using NUnit.Framework;
using System.Net;

namespace Conjur.Test
{
    [TestFixture]
    public abstract class Base
    {
        protected readonly Conjur.Client Client;
        static protected readonly WebMocker Mocker = new WebMocker();

        static Base()
        {
            WebRequest.RegisterPrefix("test", Mocker);
        }

        [SetUp]
        protected void ClearMocker()
        {
            Mocker.Clear();
            Mocker.Mock(new Uri("test:///info"), "{ \"account\": \"test-account\" }");
        }

        protected Base()
        {
            Client = new Conjur.Client("test:///");
        }
    }
}

