using System;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Conjur.Test
{
    public class UsersTest : Base
    {

        private static string accountTest = "test-account";

        public UsersTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void TestListUser()
        {
            string userListUrl = string.Format("test:///authz/{0}/resources/user", accountTest);
            IEnumerator<User> users;

            // Verify REST requests and response handling as expected. 
            // Verify offset, and limit evaluation made as expected
            ClearMocker();
            Mocker.Mock(new Uri($"{userListUrl}?offset=0&limit=1000"), GenerateUsersInfo(0, 1000));
            Mocker.Mock(new Uri($"{userListUrl}?offset=1000&limit=1000"), GenerateUsersInfo(1000, 2000));
            Mocker.Mock(new Uri($"{userListUrl}?offset=2000&limit=1000"), "[]");
            users = Client.ListUsers().GetEnumerator();
            VerifyUsersInfo(users, 2000);

            // Check handling invalid json response from conjur server
            ClearMocker();
            Mocker.Mock(new Uri($"{userListUrl}?offset=0&limit=1000"), @"[""id"":""ivnalidjson""]");
            users = Client.ListUsers().GetEnumerator();
            Assert.Throws<SerializationException>(() => users.MoveNext());
        }


        private void VerifyUsersInfo(IEnumerator<User> users, int expectedNumUsers)
        {
            for(int id = 0; id < expectedNumUsers; ++id) {
                Assert.AreEqual(true, users.MoveNext());
                Assert.AreEqual($"abc:user:{id}-admin@AutomationVault{id}", users.Current.Id);
            }
            Assert.AreEqual(false, users.MoveNext());
        }

        private string GenerateUsersInfo(int firstUserId, int lastUserId)
        {
            string res = "";

            for(int userId = firstUserId; userId < lastUserId; userId++) {
                res +=(userId == firstUserId) ? "" : ",";
                res += $"{{\"id\":\"abc:user:{userId}-admin@AutomationVault{userId}\"," +
                          $"\"owner\":\"owner{userId}\"," +
                          $"\"permissions\":[]," +
                          $"\"annotations\":[]}}";
            }
            return $"[{res}]";
        }
    }
}