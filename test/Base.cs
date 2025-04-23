namespace Conjur.Test;

[TestFixture]
public abstract class Base
{
    protected const string BaseUri = "test://example.com/";

    protected readonly Client Client;
    protected readonly string TestAccount = "test-account";
    protected readonly string LoginName = "admin";
    protected static readonly WebMocker Mocker = new();

    [SetUp]
    protected void ClearMocker()
    {
        Mocker.Clear();
        Mocker.Mock(new Uri("test:///info.com"), "{ \"account\": \"test-account\"}");
    }

    protected Base()
    {
        Client = new Client(Mocker.GetMockHttpClient(), BaseUri, TestAccount);
    }

    protected static void Verify<T>(IEnumerable<T> vars, int offset, int expectedNumVars, Action<int, T> assert) where T : Resource
    {
        using var i = vars.GetEnumerator();
        for (var id = offset; id < expectedNumVars; ++id)
        {
            Assert.AreEqual(true, i.MoveNext());
            assert(id, i.Current);
        }
        Assert.AreEqual(false, i.MoveNext());
    }

    protected static async Task VerifyAsync<T>(IAsyncEnumerable<T> vars, int offset, int expectedNumVars, Action<int, T> assert) where T : Resource
    {
        await using var i = vars.GetAsyncEnumerator();
        for (var id = offset; id < expectedNumVars; ++id)
        {
            Assert.AreEqual(true, await i.MoveNextAsync());
            assert(id, i.Current);
        }
        Assert.AreEqual(false, await i.MoveNextAsync());
    }

    protected string GenerateInfo(int firstId, int lastId, string kind)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append('[');
        for (var id = firstId; id < lastId; id++)
        {
            if (stringBuilder.Length > 1)
            {
                stringBuilder.Append(',');
            }

            stringBuilder.Append($"{{\"id\":\"{Client.AccountName}:{kind}:id{id}\"}}");
        }
        stringBuilder.Append(']');

        return stringBuilder.ToString();
    }
}
