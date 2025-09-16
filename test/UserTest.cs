using System.Runtime.Serialization;

namespace Conjur.Test;

public class UserTest : Base
{
    public UserTest()
    {
        Client.Authenticator = new MockAuthenticator();
    }

    [Test]
    public async Task ListUserTest()
    {
        var resourceUrl = $"{BaseUri}resources/{TestAccount}/{Constants.KIND_USER}";

        ClearMocker();
        Mocker.Mock(new Uri(resourceUrl + "?offset=0&limit=1000"), GenerateUsersInfo(0, 1000));
        Mocker.Mock(new Uri(resourceUrl + "?offset=1000&limit=1000"), "[]");
        var users = Client.ListUsers(null, 1000, 0);
        VerifyUserInfo(users, 0, 1000);

        var usersAsync = Client.ListUsersAsync(null, 1000, 0);
        await VerifyUserInfoAsync(usersAsync, 0, 1000);

        ClearMocker();
        Mocker.Mock(new Uri(resourceUrl + "?offset=1000&limit=1000"), GenerateUsersInfo(1000, 2000));
        Mocker.Mock(new Uri(resourceUrl + "?offset=2000&limit=1000"), "[]");
        users = Client.ListUsers(null, 1000, 1000);
        VerifyUserInfo(users, 1000, 2000);

        usersAsync = Client.ListUsersAsync(null, 1000, 1000);
        await VerifyUserInfoAsync(usersAsync, 1000, 2000);

        ClearMocker();
        Mocker.Mock(new Uri(resourceUrl + "?offset=0&limit=1000"), @"[""id"":""invalidjson""]");
        users = Client.ListUsers(null, 1000, 0);
        Assert.Throws<SerializationException>(() => _ = users.Count());

        usersAsync = Client.ListUsersAsync(null, 1000, 0);
        Assert.ThrowsAsync<SerializationException>(async () => _ = await usersAsync.CountAsync());
    }

    private void VerifyUserInfo(IEnumerable<User> users, int offset, int expectedNumUsers)
        => Verify(users, offset, expectedNumUsers, (id, u) => Assert.AreEqual($"{Client.AccountName}:{Constants.KIND_USER}:id{id}", u.Id));

    private Task VerifyUserInfoAsync(IAsyncEnumerable<User> users, int offset, int expectedNumUsers)
        => VerifyAsync(users, offset, expectedNumUsers, (id, u) => Assert.AreEqual($"{Client.AccountName}:{Constants.KIND_USER}:id{id}", u.Id));

    private string GenerateUsersInfo(int firstUserId, int lastUserId) => GenerateInfo(firstUserId, lastUserId, Constants.KIND_USER);
}
