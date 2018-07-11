using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

namespace Conjur.Test
{
    public class UserTest : Base
    {
        public UserTest()
        {
            Client.Authenticator = new MockAuthenticator();
        }

        [Test]
        public void ListUserTest()
        {
            string resourceUrl = $"test:///resources/{TestAccount}?{Constants.KIND_USER}";
            IEnumerator<User> users;

            ClearMocker();
            Mocker.Mock(new Uri(resourceUrl + "&offset=0&limit=1000"), GenerateUsersInfo(0, 1000));
            Mocker.Mock(new Uri(resourceUrl + "&offset=1000&limit=1000"), GenerateUsersInfo(1000, 2000));
            Mocker.Mock(new Uri(resourceUrl + "&offset=2000&limit=1000"), "[]");
            users = Client.ListUsers().GetEnumerator();
            verifyUserInfo(users, 2000);

            ClearMocker();
            Mocker.Mock(new Uri(resourceUrl + "&offset=0&limit=1000"), @"[""id"":""invalidjson""]");
            users = Client.ListUsers().GetEnumerator();
            Assert.Throws<SerializationException> (() => users.MoveNext());

        }

        private void verifyUserInfo(IEnumerator<User> users, int excpectedNumUsers)
        {
            for (int id = 0; id < excpectedNumUsers; ++id) 
            {
                Assert.AreEqual(true, users.MoveNext());
                Assert.AreEqual($"{Client.GetAccountName()}:{Constants.KIND_USER}:id{id}", users.Current.Id);
            }
            Assert.AreEqual(false, users.MoveNext());
        }

        private string GenerateUsersInfo(int firstUserId, int lastUserId)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int userId = firstUserId; userId < lastUserId; userId++)
            {
                stringBuilder.Append($"{{\"id\":\"{Client.GetAccountName()}:{Constants.KIND_USER}:id{userId}\"}},");
            }
            if (stringBuilder.Length != 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            return $"[{stringBuilder}]";
        }
    }
}
