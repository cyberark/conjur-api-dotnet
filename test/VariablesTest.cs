using System.Runtime.Serialization;

namespace Conjur.Test;

public class VariablesTest : Base
{
    public VariablesTest()
    {
        Client.Authenticator = new MockAuthenticator();
    }

    [Test]
    public async Task GetVariableTest()
    {
        //test:/secrets/test-account/variable/foo%2Fbar
        Mocker.Mock(new Uri($"{BaseUri}secrets/{TestAccount}/variable/foo%2Fbar"), "test-value");
        Assert.AreEqual("test-value", Client.Variable("foo/bar").GetValue());
        Assert.AreEqual("test-value", await Client.Variable("foo/bar").GetValueAsync());

        Mocker.Mock(new Uri($"{BaseUri}secrets/{TestAccount}/variable/foo%20bar"), "space test");
        Assert.AreEqual("space test", Client.Variable("foo bar").GetValue());
        Assert.AreEqual("space test", await Client.Variable("foo bar").GetValueAsync());
    }

    [Test]
    public async Task AddSecretTest()
    {
        char[] testValue = ['t', 'e', 's', 't', 'V', 'a', 'l', 'u', 'e'];

        var v = Mocker.Mock(new Uri($"{BaseUri}secrets/{TestAccount}/variable/foobar"), "");
        v.Verifier = request =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("text/plain", request.Content.Headers.ContentType.MediaType);
            using (StreamReader sr = new StreamReader(request.Content.ReadAsStream()))
            {
                // Read from the beginning of the request content
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                Assert.AreEqual(testValue, sr.ReadToEnd());
            }
        };
        Client.Variable("foobar").AddSecret(Encoding.UTF8.GetBytes(testValue));

        await Client.Variable("foobar").AddSecretAsync(Encoding.UTF8.GetBytes(testValue));
    }

    [Test]
    public async Task ListVariableTest()
    {
        var variableUri = $"{BaseUri}resources/{TestAccount}/{Constants.KIND_VARIABLE}";

        ClearMocker();
        Mocker.Mock(new Uri(variableUri + "?offset=0&limit=1000"), GenerateVariablesInfo(0, 1000));
        Mocker.Mock(new Uri(variableUri + "?offset=1000&limit=1000"), "[]");
        var vars = Client.ListVariables(null, 1000, 0);
        VerifyVariablesInfo(vars, 0, 1000);

        var varsAsync = Client.ListVariablesAsync(null, 1000, 0);
        await VerifyVariablesInfo(varsAsync, 0, 1000);

        ClearMocker();
        Mocker.Mock(new Uri(variableUri + "?offset=1000&limit=1000"), GenerateVariablesInfo(1000, 2000));
        Mocker.Mock(new Uri(variableUri + "?offset=2000&limit=1000"), "[]");
        vars = Client.ListVariables(null, 1000, 1000);
        VerifyVariablesInfo(vars, 1000, 2000);

        varsAsync = Client.ListVariablesAsync(null, 1000, 1000);
        await VerifyVariablesInfo(varsAsync, 1000, 2000);

        ClearMocker();
        Mocker.Mock(new Uri(variableUri + "?offset=0&limit=1000"), @"[""id"":""invalidjson""]");
        vars = Client.ListVariables(null, 1000, 0);
        Assert.Throws<SerializationException>(() => _ = vars.Count());

        varsAsync = Client.ListVariablesAsync(null, 1000, 0);
        Assert.ThrowsAsync<SerializationException>(async () => _ = await varsAsync.CountAsync());
    }

    [Test]
    public async Task CountVariablesTest()
    {
        var variableUri = $"{BaseUri}resources/{TestAccount}/{Constants.KIND_VARIABLE}";

        ClearMocker();
        Mocker.Mock(new Uri(variableUri + "?count=true&search=dummy"), @"{""count"":10}");

        Assert.AreEqual(10, Client.CountVariables("dummy"));
        Assert.AreEqual(10, await Client.CountVariablesAsync("dummy"));
    }

    private void VerifyVariablesInfo(IEnumerable<Variable> vars, int offset, int expectedNumVars)
        => Verify(vars, offset, expectedNumVars, (id, v) => Assert.AreEqual($"{Client.AccountName}:{Constants.KIND_VARIABLE}:id{id}", v.Id));

    private Task VerifyVariablesInfo(IAsyncEnumerable<Variable> vars, int offset, int expectedNumVars)
        => VerifyAsync(vars, offset, expectedNumVars, (id, v) => Assert.AreEqual($"{Client.AccountName}:{Constants.KIND_VARIABLE}:id{id}", v.Id));

    private string GenerateVariablesInfo(int firstVarId, int lastVarId) => GenerateInfo(firstVarId, lastVarId, Constants.KIND_VARIABLE);
}
