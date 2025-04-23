namespace Conjur.Test;

public class AWSAuthenticatorTest : Base
{
    [Test]
    public void AuthenticateTest()
    {
        // Check if we're running in an AWS environment. This environment variable is set by Summon when run in the Jenkins pipeline,
        // since it's defined in secrets.yml under the 'ci' environment. If it's not set, we ignore the test.
        if (Environment.GetEnvironmentVariable("RUN_AWS_TESTS") != "true")
        {
            Assert.Ignore("This test requires an AWS environment to run. Set the RUN_AWS_TESTS environment variable to 'true' to run this test.");
        }

        const string conjurIdentity = "somehost";
        const string conjurAuthenticator = "authn-iam/test";
        // This role must be the role assigned to the EC2 instance where this test is running.
        // Here it is set to the role used by Jenkins. When running this test manually in Visual Studio,
        // replace this with a role that the machine has permissions to assume.
        const string roleArnToAssume = "arn:aws:iam::601277729239:role/authn-iam-test-role-a";

        var awsAuthenticator = new AWSIAMAuthenticator(
            Mocker.GetMockHttpClient(),
            Client.ApplianceUri.ToString(),
            Client.AccountName,
            conjurIdentity,
            conjurAuthenticator,
            roleArnToAssume
        );

        // Mock the response from the Conjur server
        const string mockToken = "iam_token";
        var mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockToken));
        var m = Mocker.Mock(new Uri(Client.ApplianceUri, "authn-iam/test/test-account/somehost/authenticate"), mockTokenEncoded);
        m.Verifier = request =>
        {
            // Verify that the request was made with the correct parameters
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType);
            Assert.IsInstanceOf<JsonContent>(request.Content);
            JsonContent content = (JsonContent)request.Content;
            Assert.IsInstanceOf<ConjurAWSIAMAuth>(content.Value);
        };

        var token = awsAuthenticator.GetToken();
        Assert.AreEqual(mockToken, token);
    }
}
