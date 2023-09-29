// <copyright file="AWSIAMAuthenticator.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2023 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     AWS IAM authenticator.
// </summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// https://www.nuget.org/packages/AWSSDK.SecurityToken -- needed for STS
// dotnet add package AWSSDK.SecurityToken --version 3.7.201.16
using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;

// https://www.nuget.org/packages/Aws4RequestSigner -- simplify signing of the request
// dotnet add package Aws4RequestSigner --version 1.0.3
using Aws4RequestSigner;

namespace Conjur
{
    /// <summary>
    /// AWS IAM authenticator.
    /// </summary>
    public class AWSIAMAuthenticator : IAuthenticator
    {
        private string token = string.Empty;
        private string conjurIdentity = string.Empty;
        private string conjurAccount = string.Empty;
        private string conjurAuthenticator = string.Empty;
        private Uri conjurApiUri;
        private string conjurAWSRegion = string.Empty; // AWS Region where Conjur is running (default is "us-east-1")
        private string conjurIAMRole = string.Empty;

        public bool Debug = false;

        public AWSIAMAuthenticator(string conjurUri, string conjurAccount, string Identity, string Authenticator, string roleArn = "", string ConjurAWSRegion = "us-east-1")
        {
            this.conjurApiUri = new Uri(conjurUri);
            this.conjurAccount = conjurAccount;
            this.conjurIdentity = Identity;
            this.conjurAuthenticator = Authenticator;
            this.conjurIAMRole = roleArn;
            this.conjurAWSRegion = ConjurAWSRegion;
        }

        #region IAuthenticator implementation

        /// <summary>
        /// Obtain a Conjur authentication token based on AWS STS session credentials
        /// </summary>
        /// <returns>Conjur authentication token in verbatim form.
        /// It will be base64-encoded in the Client.</returns>
        public string GetToken()
        {
            Credentials stsCreds = GetSTSToken();
            HttpRequestMessage signedRequest = CreateSignedRequest(stsCreds);
            ConjurAWSIAMAuth conjAuth = CreateConjurAWSIAMAuth(signedRequest);

            if (this.Debug)
            {
                this.RenderCurlCommand(conjAuth);
            }

            string conjToken = GetConjurToken(conjAuth);
            return conjToken;
        }
        #endregion

        private Credentials GetSTSToken()
        {
            var region = RegionEndpoint.GetBySystemName(this.conjurAWSRegion);
            var stsClient = new Amazon.SecurityToken.AmazonSecurityTokenServiceClient(region);

            if (this.Debug)
            {
                var callerIdRequest = new GetCallerIdentityRequest();
                var caller = FetchCallerIdentityAsync(stsClient, callerIdRequest).Result;
                Console.WriteLine("ARN: {0}", caller.Arn);
            }

            if (!String.IsNullOrEmpty(this.conjurIAMRole))
            {
                var assumeRoleReq = new AssumeRoleRequest()
                {
                    DurationSeconds = 900, // AWS SDK minimum value
                    RoleSessionName = "Session1",
                    RoleArn = this.conjurIAMRole
                };

                AssumeRoleResponse assumeRoleRes;
                try
                {
                    assumeRoleRes = FetchAssumeRoleAsync(stsClient, assumeRoleReq).Result;
                    if (this.Debug)
                    {
                        var callerIdRequest2 = new GetCallerIdentityRequest();
                        var caller2 = FetchCallerIdentityAsync(stsClient, callerIdRequest2).Result;
                        Console.WriteLine("ARN: {0}", caller2.Arn);
                    }

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
                DurationSeconds = 120 // 2 minutes
            };

            GetSessionTokenResponse sessionTokenResponse = FetchTokenAsync(stsClient, getSessionTokenRequest).Result;
            Credentials temporaryCredentials = sessionTokenResponse.Credentials;
            return temporaryCredentials;
        }

        private HttpRequestMessage CreateSignedRequest(Credentials sessionCredentials)
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

            var signedRequest = signer.Sign(request, "sts", this.conjurAWSRegion).ConfigureAwait(false).GetAwaiter().GetResult();
            return signedRequest;
        }
        private ConjurAWSIAMAuth CreateConjurAWSIAMAuth(HttpRequestMessage signedRequest)
        {
            string authString = string.Empty;
            string dateString = string.Empty;
            string sha256String = string.Empty;
            string reqHost = string.Empty;
            string token = string.Empty;

            IEnumerable<string> headerSearchResults;
            if (signedRequest.Headers.TryGetValues("Authorization", out headerSearchResults))
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
            var conjauth = new ConjurAWSIAMAuth
            {
                Authorization = authString,
                Date = dateString,
                SecurityToken = token,
                ContentSha256Token = sha256String,
                Host = reqHost
            };

            return conjauth;
        }

        public void RenderCurlCommand(ConjurAWSIAMAuth conjAuth)
        {
            string curlCmd = "curl -D - -k -H 'Content-Type: application/json; charset=utf-8' -H 'Accept-Encoding: base64'";
            curlCmd += string.Format(" -H 'Authorization: {0}'", conjAuth.Authorization);
            curlCmd += string.Format(" -H 'x-amz-date: {0}'", conjAuth.Date);
            curlCmd += string.Format(" -H 'x-amz-content-sha256: {0}'", conjAuth.ContentSha256Token);
            curlCmd += string.Format(" -H 'x-amz-security-token: {0}'", conjAuth.SecurityToken);
            curlCmd += string.Format(" -H 'host: {0}'", conjAuth.Host);
            curlCmd += string.Format(" \"https://sts.amazonaws.com/?Action=GetCallerIdentity&Version=2011-06-15\"");
            Console.WriteLine(curlCmd);

            var jsonstuff = JsonContent.Create(conjAuth);
            var json = new StreamReader(jsonstuff.ReadAsStream()).ReadToEnd();
            Console.WriteLine("curl -D - -k -H 'Content-Type: application/json; charset=utf-8' -H 'Accept-Encoding: base64' \"{0}\" -d '{1}'", this.GetConjurAuthenticateUrl(), json);
        }
        public string GetConjurAuthenticateUrl()
        {
            string conjId = System.Uri.EscapeDataString(this.conjurIdentity);

            // https://docs.conjur.org/Latest/en/Content/Developer/Conjur_API_Authenticate.htm
            // POST /{authenticator}/{account}/{login}/authenticate
            string apiUrl = this.conjurApiUri.ToString();
            apiUrl = apiUrl.EndsWith("/") ? apiUrl.Substring(0, apiUrl.Length - 1) : apiUrl;

            string url = string.Format("{0}/{1}/{2}/{3}/authenticate",
                                    apiUrl,
                                    this.conjurAuthenticator,
                                    this.conjurAccount,
                                    conjId);
            return url;
        }
        public string GetConjurToken(ConjurAWSIAMAuth conjAuth)
        {
            string url = this.GetConjurAuthenticateUrl();

            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var client = new HttpClient(handler);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(conjAuth)
            };
            httpRequestMessage.Headers.AcceptEncoding.TryParseAdd("base64");
            HttpResponseMessage response = client.Send(httpRequestMessage);

            using var reader = new StreamReader(response.Content.ReadAsStream());
            var stringBase64Response = reader.ReadToEnd();

            byte[] data = Convert.FromBase64String(stringBase64Response);
            string decodedString = System.Text.Encoding.UTF8.GetString(data);
            return decodedString;
        }

        private static async Task<GetSessionTokenResponse> FetchTokenAsync(Amazon.SecurityToken.AmazonSecurityTokenServiceClient stsClient, GetSessionTokenRequest request)
        {
            var response = await stsClient.GetSessionTokenAsync(request);
            return response;
        }

        private static async Task<AssumeRoleResponse> FetchAssumeRoleAsync(Amazon.SecurityToken.AmazonSecurityTokenServiceClient client, AssumeRoleRequest request)
        {
            var response = await client.AssumeRoleAsync(request);
            return response;
        }
        private static async Task<GetCallerIdentityResponse> FetchCallerIdentityAsync(Amazon.SecurityToken.AmazonSecurityTokenServiceClient client, GetCallerIdentityRequest request)
        {
            var response = await client.GetCallerIdentityAsync(request);
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
}
