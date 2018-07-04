using System;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;
using static Conjur.Test.WebMocker;
using System.Text;
using System.Linq;

namespace Conjur.Test
{
    public class RoleTest : Base
    {
        private string baseUrl = "test:///authz/test-account/roles/group";
        
        public RoleTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestRoleDoesExist()
        {
            ClearMocker();
            Mocker.Mock(new Uri($"{baseUrl}/groupName"), "");
            Assert.IsTrue(Client.Role("group", "groupName").Exists(), "Group 'groupName' expected to be exist");
        }

        [Test]
        public void TestRoleDoesNotExist()
        {
            ClearMocker();
            MockRequest mock = Mocker.Mock(new Uri($"{baseUrl}/groupName"), "");
            mock.Verifier = (wr) => 
            {
                throw new WebMocker.MockResponseException(HttpStatusCode.NotFound, "NotFound");
            };

            Assert.IsFalse(Client.Role("group", "groupName").Exists(), "Group 'groupName' expected to be not exist");
        }

        [Test]
        public void TestRoleMembers()
        {
            ClearMocker();
            Mocker.Mock(new Uri($"{baseUrl}/groupName?members"), GenerateMembersData(10));
            VerifyRoleInfo(Client.Role("group", "groupName").ListMembers().GetEnumerator(), 10);
        }

        [Test]
        public void TestRoleMemberships()
        {
            ClearMocker();
            Mocker.Mock(new Uri($"{baseUrl}/groupName?all"), "[ \"role0\", \"role1\" ]");
            string[] roles = Client.Role("group", "groupName").ListMemberships().ToArray();
            Assert.AreEqual("role0", roles[0]);
            Assert.AreEqual("role1", roles[1]);
        }

        private void VerifyRoleInfo(IEnumerator<RoleMember> members, int expectedNum)
        {
            for(int id = 0; id < expectedNum; id++)
            {
                Assert.AreEqual(true, members.MoveNext());
                Assert.AreEqual($"theMember{id}", members.Current.Member);
            }
            Assert.AreEqual(false, members.MoveNext());
        }

        private string GenerateMembersData(int count)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for(int id = 0; id < count; id++) 
            {
                stringBuilder.Append($"{{\"admin_option\": {(id % 2 == 0).ToString().ToLower()},");
                stringBuilder.Append($"\"grantor\":\"theGrantor\",");
                stringBuilder.Append($"\"member\":\"theMember{id}\",");
                stringBuilder.Append($"\"role\":\"theRole\"}}");
                stringBuilder.Append(",");
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            return $"[{stringBuilder}]";
        }
    }
}