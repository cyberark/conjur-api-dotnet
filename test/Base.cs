using System;
using NUnit.Framework;
using System.Net;

namespace Conjur.Test
{
    [TestFixture]
    public abstract class Base
    {
        protected readonly Conjur.Client Client;
        protected readonly string TestAccount = "test-account";
        static protected readonly WebMocker Mocker = new WebMocker();

        static Base()
        {
            WebRequest.RegisterPrefix("test", Mocker);
        }

        protected Base()
        {
            Client = new Conjur.Client("test:///", TestAccount);
        }
    }
}
