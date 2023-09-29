using NUnit.Framework;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace Conjur.Test
{
    public class AWSAuthenticatorTest : Base
    {
        [Test]
        public void AuthenticateTest()
        {
            string conjurIdentity = "somehost";
            string conjurAuthenticator = "authn-iam/test";
            // This role must be the role assigned to the EC2 instance where this test is running.
            // Here it is set to the role used by Jenkins. When running this test manually in Visual Studio,
            // replace this with a role that the machine has permissions to assume.
            string roleArnToAssume = "arn:aws:iam::601277729239:role/authn-iam-test-role-a";

            AWSIAMAuthenticator awsAuthenticator = new AWSIAMAuthenticator(
                this.Client.ApplianceUri.ToString(),
                this.Client.GetAccountName(),
                conjurIdentity,
                conjurAuthenticator,
                roleArnToAssume
            );
            awsAuthenticator.httpClient = Mocker.GetMockHttpClient();

            // Mock the response from the Conjur server
            string mockToken = "iam_token";
            string mockTokenEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(mockToken));
            var m = Mocker.Mock(new Uri(this.Client.ApplianceUri, "authn-iam/test/test-account/somehost/authenticate"), mockTokenEncoded);
            m.Verifier = request =>
            {
                // Verify that the request was made with the correct parameters
                Assert.AreEqual(HttpMethod.Post, request.Method);
                Assert.AreEqual("application/json", request.Content.Headers.ContentType.MediaType);
                Assert.IsInstanceOf<JsonContent>(request.Content);
                JsonContent content = (JsonContent)request.Content;
                Assert.IsInstanceOf<ConjurAWSIAMAuth>(content.Value);
            };

            string token = awsAuthenticator.GetToken();
            Assert.AreEqual(mockToken, token);
        }
    }
}
