﻿using System;
using System.Linq;
using System.Net.Http;
using NUnit.Framework;

namespace Conjur.Test
{
    public class HostFactoryTest : Base
    {
        [Test]
        public void TestCreateHost()
        {
            Mocker.Mock(new Uri("test:///host_factories/hosts?id=test-host"),
                @"{
                   ""id"": ""test-host"",
                   ""userid"": ""deputy/test-factory"",
                   ""created_at"": ""2015-11-13T22:57:14Z"",
                   ""ownerid"": ""test:group:ops"",
                   ""roleid"": ""test:host:test-host"",
                   ""resource_identifier"": ""test:host:test-host"",
                   ""api_key"": ""14x82x72syhnnd1h8jj24zj1kqd2j09sjy3tddwxc35cmy5nx33ph7""
               }")
                .Verifier = (HttpRequestMessage requestMessage) =>
            {
                Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
                if (requestMessage.Headers.GetValues("Authorization").SingleOrDefault() != "Token token=\"host-factory-token\"")
                {
                    throw new UnauthorizedException("Unauthorized", new HttpRequestException("Unauthorized"));
                }
            };

            Assert.Throws<UnauthorizedException>(
                () => Client.CreateHost("test-host", "bar"));

            var host = Client.CreateHost("test-host", "host-factory-token");

            Assert.AreEqual("test-host", host.Id);
            Assert.AreEqual("host/test-host", host.Credential.UserName);
            Assert.AreEqual(
                "14x82x72syhnnd1h8jj24zj1kqd2j09sjy3tddwxc35cmy5nx33ph7",
                host.Credential.Password);
        }
    }
}