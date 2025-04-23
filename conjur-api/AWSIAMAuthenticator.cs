// <copyright file="AWSIAMAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     AWS IAM authenticator.
// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Aws4RequestSigner;

namespace Conjur;

/// <summary>
/// AWS IAM authenticator.
/// </summary>
public class AWSIAMAuthenticator(
                HttpClient httpClient,
                string conjurUri,
                string conjurAccount,
                string identity,
                string authenticator,
                string roleArn = "",
                string conjurAWSRegion = "us-east-1") : IAuthenticator
{
    private readonly Uri conjurApiUri = new(conjurUri);

    public AWSIAMAuthenticator(
        string conjurUri,
        string conjurAccount,
        string identity,
        string authenticator,
        string roleArn = "",
        string conjurAWSRegion = "us-east-1") : this(new HttpClient(), conjurUri, conjurAccount, identity, authenticator, roleArn, conjurAWSRegion)
    {
    }

    #region IAuthenticator implementation

    /// <summary>
    /// Obtain a Conjur authentication token based on AWS STS session credentials
    /// </summary>
    /// <returns>Conjur authentication token in verbatim form.
    /// It will be base64-encoded in the Client.</returns>
    public string GetToken() => GetTokenAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Obtain a Conjur authentication token based on AWS STS session credentials
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Conjur authentication token in verbatim form.</returns>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var stsCredentials = await GetSTSTokenAsync(cancellationToken);
        var signedRequest = await CreateSignedRequestAsync(stsCredentials);
        var conjAuth = CreateConjurAWSIAMAuth(signedRequest);

        return await GetConjurTokenAsync(conjAuth, cancellationToken);
    }

    #endregion

    private async Task<Credentials> GetSTSTokenAsync(CancellationToken cancellationToken)
    {
        var region = RegionEndpoint.GetBySystemName(conjurAWSRegion);
        var stsClient = new AmazonSecurityTokenServiceClient(region);

        // TODO: Handle case where we're running on an EC2 instance that has the assigned role equal to the role
        // used to authenticate to Conjur. Then we don't need to assume a different role, and we already have
        // the session token we need.
        // For now this can be handled by assuming the same role and allowing the AssumeRole permission for the role
        // for itself.

        if (!string.IsNullOrEmpty(roleArn))
        {
            var assumeRoleReq = new AssumeRoleRequest
            {
                DurationSeconds = 900, // AWS SDK minimum value
                RoleSessionName = "Session1",
                RoleArn = roleArn
            };

            AssumeRoleResponse assumeRoleRes;
            try
            {
                assumeRoleRes = await FetchAssumeRoleAsync(stsClient, assumeRoleReq, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            return assumeRoleRes.Credentials;
        }

        var getSessionTokenRequest = new GetSessionTokenRequest
        {
            DurationSeconds = 900 // AWS SDK minimum value
        };

        var sessionTokenResponse = await FetchTokenAsync(stsClient, getSessionTokenRequest, cancellationToken);
        var temporaryCredentials = sessionTokenResponse.Credentials;
        return temporaryCredentials;
    }

    private async Task<HttpRequestMessage> CreateSignedRequestAsync(Credentials sessionCredentials)
    {
        var signer = new AWS4RequestSigner(sessionCredentials.AccessKeyId, sessionCredentials.SecretAccessKey);
        var content = new StringContent("", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://sts.amazonaws.com/?Action=GetCallerIdentity&Version=2011-06-15"),
            Content = content
        };
        request.Headers.Add("X-Amz-Security-Token", sessionCredentials.SessionToken);

        return await signer.Sign(request, "sts", conjurAWSRegion);
    }

    public void RenderCurlCommand(ConjurAWSIAMAuth conjAuth)
    {
        var curlCmd = $"""
                       curl -D - -k
                        -H 'Content-Type: application/json; charset=utf-8'
                        -H 'Accept-Encoding: base64'
                        -H 'Authorization: {conjAuth.Authorization}'
                        -H 'x-amz-date: {conjAuth.Date}'
                        -H 'x-amz-content-sha256: {conjAuth.ContentSha256Token}'
                        -H 'x-amz-security-token: {conjAuth.SecurityToken}'
                        -H 'host: {conjAuth.Host}'
                        "https://sts.amazonaws.com/?Action=GetCallerIdentity&Version=2011-06-15"
                       """.ReplaceLineEndings(string.Empty);
        Console.WriteLine(curlCmd);

        var json = JsonSerializer.Serialize(conjAuth);
        Console.WriteLine($"""
                           curl -D - -k
                            -H 'Content-Type: application/json; charset=utf-8'
                            -H 'Accept-Encoding: base64'
                            "{GetConjurAuthenticateUrl()}"
                            -d '{json}'
                           """.ReplaceLineEndings(string.Empty));
    }

    private static ConjurAWSIAMAuth CreateConjurAWSIAMAuth(HttpRequestMessage signedRequest)
    {
        var authString = string.Empty;
        var dateString = string.Empty;
        var sha256String = string.Empty;
        var reqHost = string.Empty;
        var token = string.Empty;

        if (signedRequest.Headers.TryGetValues("Authorization", out var headerSearchResults))
        {
            authString = headerSearchResults.First();
        }
        if (signedRequest.Headers.TryGetValues("x-amz-date", out headerSearchResults))
        {
            dateString = headerSearchResults.First();
        }
        if (signedRequest.Headers.TryGetValues("x-amz-content-sha256", out headerSearchResults))
        {
            sha256String = headerSearchResults.First();
        }
        if (signedRequest.Headers.TryGetValues("host", out headerSearchResults))
        {
            reqHost = headerSearchResults.First();
        }
        if (signedRequest.Headers.TryGetValues("x-amz-security-token", out headerSearchResults))
        {
            token = headerSearchResults.First();
        }
        var conjAuth = new ConjurAWSIAMAuth
        {
            Authorization = authString,
            Date = dateString,
            SecurityToken = token,
            ContentSha256Token = sha256String,
            Host = reqHost
        };

        return conjAuth;
    }

    public string GetConjurAuthenticateUrl()
    {
        var conjId = Uri.EscapeDataString(identity);

        // https://docs.conjur.org/Latest/en/Content/Developer/Conjur_API_Authenticate.htm
        // POST /{authenticator}/{account}/{login}/authenticate
        var apiUrl = conjurApiUri.ToString();
        apiUrl = apiUrl.EndsWith('/') ? apiUrl[..^1] : apiUrl;

        return $"{apiUrl}/{authenticator}/{conjurAccount}/{conjId}/authenticate";
    }

    private async Task<string> GetConjurTokenAsync(ConjurAWSIAMAuth conjAuth, CancellationToken cancellationToken)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetConjurAuthenticateUrl())
        {
            Content = JsonContent.Create(conjAuth)
        };
        httpRequestMessage.Headers.AcceptEncoding.TryParseAdd("base64");
        var stringBase64Response = await httpClient.SendAsync(httpRequestMessage, cancellationToken).ReadAsync(cancellationToken);

        var data = Convert.FromBase64String(stringBase64Response);
        var decodedString = Encoding.UTF8.GetString(data);
        return decodedString;
    }

    private static async Task<GetSessionTokenResponse> FetchTokenAsync(AmazonSecurityTokenServiceClient stsClient, GetSessionTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await stsClient.GetSessionTokenAsync(request, cancellationToken);
        return response;
    }

    private static async Task<AssumeRoleResponse> FetchAssumeRoleAsync(AmazonSecurityTokenServiceClient client, AssumeRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await client.AssumeRoleAsync(request, cancellationToken);
        return response;
    }

    private static async Task<GetCallerIdentityResponse> FetchCallerIdentityAsync(AmazonSecurityTokenServiceClient client, GetCallerIdentityRequest request, CancellationToken cancellationToken)
    {
        var response = await client.GetCallerIdentityAsync(request, cancellationToken);
        return response;
    }
}

// This class is used to create the Conjur post body; it is JSON serialized and posted to Conjur
public class ConjurAWSIAMAuth
{
    [JsonPropertyName("Authorization")]
    public string Authorization { get; set; }

    [JsonPropertyName("x-amz-date")]
    public string Date { get; set; }

    [JsonPropertyName("x-amz-content-sha256")]
    public string ContentSha256Token { get; set; }

    [JsonPropertyName("x-amz-security-token")]
    public string SecurityToken { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }
}
