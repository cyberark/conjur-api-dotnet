using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Conjur.Test;

public class ClientTest : Base
{
    [TearDown]
    public void ResetClientState()
    {
        Client.Authenticator = null;
        Client.EnableCertCheck();
        Client.TrustedCertificates.Clear();
    }

    [Test]
    public void TestInfo()
    {
        Assert.AreEqual(TestAccount, Client.AccountName);
    }

    [Test]
    public async Task TestLogin()
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/" + TestAccount + "/login"), "api-key").Verifier =
            (HttpRequestMessage requestMessage) => Assert.AreEqual("Basic YWRtaW46c2VjcmV0", requestMessage.Headers.GetValues("Authorization").SingleOrDefault());

        var apiKey = Client.LogIn("admin", "secret");
        Assert.AreEqual("api-key", apiKey);
        VerifyAuthenticator(Client.Authenticator);

        apiKey = await Client.LogInAsync("admin", "secret");
        Assert.AreEqual("api-key", apiKey);
        VerifyAuthenticator(Client.Authenticator);
    }

    private void VerifyAuthenticator(IAuthenticator authenticator)
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/" + TestAccount + "/" + LoginName + "/authenticate"), "token")
            .Verifier = (HttpRequestMessage requestMessage) =>
        {
            Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
            Assert.AreEqual("api-key", requestMessage.Content.ReadAsStringAsync().Result);
        };
        Assert.AreEqual("token", authenticator.GetToken());
    }

    [Test]
    public void TestAuthenticatedRequest()
    {
        Mocker.Mock(new Uri("test://info.com"), "{ \"account\": \"test-account\" }");
        Client.Authenticator = new MockAuthenticator();
        var testRequest = Client.AuthenticatedRequest("info");
        Assert.AreEqual("Token token=\"dG9rZW4=\"", // "token" base64ed
            testRequest.Headers.GetValues("Authorization").Single());

        Client.Authenticator = null;
        Assert.Throws<InvalidOperationException>(() => Client.AuthenticatedRequest("info"));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Client.AuthenticatedRequestAsync("info", CancellationToken.None));
    }

    [Test]
    public async Task ActingAsTest()
    {
        // Test in mono fails when using : in role variable. role should be TestAccount:Kind:foo
        const string role = "foo";
        var resourceVarUri = $"{BaseUri}resources/{TestAccount}/{Constants.KIND_VARIABLE}";
        string[] expected = [$"{Client.AccountName}:{Constants.KIND_VARIABLE}:id"];

        Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000&acting_as={role}"), $"[{{\"id\":\"{Client.AccountName}:{Constants.KIND_VARIABLE}:id\"}}]");
        Mocker.Mock(new Uri($"{resourceVarUri}?offset=0&limit=1000"), "[]");

        Client.Authenticator = new MockAuthenticator();

        var actingAsClientVars = Client.ActingAs(role).ListVariables(null, 1000, 0).ToList();
        var plainClientVars = Client.ListVariables(null, 1000, 0).ToList();

        CollectionAssert.AreEqual(expected, actingAsClientVars.Select(x => x.Id).ToArray());
        CollectionAssert.IsEmpty(plainClientVars);

        actingAsClientVars = await Client.ActingAs(role).ListVariablesAsync(null, 1000, 0).ToListAsync();
        plainClientVars = await Client.ListVariablesAsync(null, 1000, 0).ToListAsync();

        CollectionAssert.AreEqual(expected, actingAsClientVars.Select(x => x.Id).ToArray());
        CollectionAssert.IsEmpty(plainClientVars);
    }

    [Test]
    public async Task CreatePolicyTest()
    {
        const string policyResponseText = "{\"created_roles\":{},\"version\":10}";
        const string policyId = "vaultname/policyname";
        var policyPath = $"{BaseUri}policies/{Client.AccountName}/{Constants.KIND_POLICY}";

        // notice: We must encode policyId, 
        Mocker.Mock(new Uri($"{policyPath}/{Uri.EscapeDataString(policyId)}"), policyResponseText);

        Client.Authenticator = new MockAuthenticator();

        await using (var policyStream = CreatePolicyStream())
        {
            var responseStream = Client.Policy(policyId).LoadPolicy(policyStream);
            AssertPolicyResponse(responseStream);
        }

        await using (var policyStream = CreatePolicyStream())
        {
            var responseStream = await Client.Policy(policyId).LoadPolicyAsync(policyStream);
            AssertPolicyResponse(responseStream);
        }

        Stream CreatePolicyStream()
        {
            var ms = new MemoryStream();
            using var sw = new StreamWriter(ms, leaveOpen: true);
            sw.WriteLine("- !variable");
            sw.WriteLine("  id: TestVariable");
            sw.Flush();
            return ms;
        }

        void AssertPolicyResponse(Stream responseStream)
        {
            try
            {
                using var reader = new StreamReader(responseStream);
                Assert.AreEqual(policyResponseText, reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                Assert.Fail("Failure in policy load response: " + ex);
            }
        }
    }

    [Test]
    public void TestGetTelemetryHeader_WithAllValues()
    {
        // Arrange: Set all properties to ensure full header construction
        string expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("in=MyIntegration&it=custom-type&iv=1.0.0&vn=MyVendor&vv=1.0")).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        Client.IntegrationName = "MyIntegration";
        Client.IntegrationType = "custom-type";
        Client.IntegrationVersion = "1.0.0";
        Client.VendorName = "MyVendor";
        Client.VendorVersion = "1.0";

        // Act
        string result = Client.GetTelemetryHeader();

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void TestTelemetryHeaderCaching()
    {
        // Arrange: Set initial values and get the telemetry header
        Client.IntegrationName = "MyIntegration";
        Client.IntegrationType = "custom-type";
        Client.IntegrationVersion = "1.0.0";
        Client.VendorName = "MyVendor";
        Client.VendorVersion = "1.0";
        
        string firstHeader = Client.GetTelemetryHeader();

        // Act: Change a property and check if the header changes
        Client.IntegrationName = "NewIntegration";
        string secondHeader = Client.GetTelemetryHeader();

        // Assert: Ensure header changes after a property change
        Assert.AreNotEqual(firstHeader, secondHeader);
    }

    [Test]
    public void TestGetTelemetryHeader_WithNullVendorVersion()
    {
        // Arrange: Set properties, leaving vendor version as null
        Client.IntegrationName = "MyIntegration";
        Client.IntegrationType = "custom-type";
        Client.IntegrationVersion = "1.0.0";
        Client.VendorName = "MyVendor";
        Client.VendorVersion = null;  // Vendor version is null
        
        string expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("in=MyIntegration&it=custom-type&iv=1.0.0&vn=MyVendor")).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        
        // Act
        string result = Client.GetTelemetryHeader();

        // Assert
        Assert.AreEqual(expected, result);
    }

    [Test]
    public void TestHttpClientTelemetryHeaderAdded()
    {
        // Arrange: Mock the GetTelemetryHeader method to return a fixed value
        string mockTelemetryHeader = "aW49TXlJbnRlZ3JhdGlvbiZpdD1jdXN0b20tdHlwZSZpdj0xLjAuMCZ2bj1NeVZlbmRvciZ2dj0xLjA";
        Client.IntegrationName = "MyIntegration";
        Client.IntegrationType = "custom-type";
        Client.IntegrationVersion = "1.0.0";
        Client.VendorName = "MyVendor";
        Client.VendorVersion = "1.0";

        // Act: Create the Client object (which will add the header)
        var client = new Client("https://example.com", "test-account");

        // Assert: Verify that the "x-cybr-telemetry" header was added to the HttpClient's DefaultRequestHeaders
        var headerValue = client.httpClient.DefaultRequestHeaders.GetValues("x-cybr-telemetry").FirstOrDefault();
        Assert.AreEqual(mockTelemetryHeader, headerValue);
    }

    [TestCase("https://example.com//test/of/test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test////of//test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test/", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com//////test////of/////test//", ExpectedResult = "https://example.com/test/of/test/")]
    [TestCase("https://example.com/test/of/test?q=p//s", ExpectedResult = "https://example.com/test/of/test/?q=p//s")]
    [TestCase("https://example.com/test/of/test?q=p%2F%2Fs", ExpectedResult = "https://example.com/test/of/test/?q=p%2F%2Fs")]
    public string TestNormalizeBaseUri(string uri) => Client.NormalizeBaseUri(uri).ToString();

    [Test]
    public void TestConstructor_ApplianceUriAndAccount()
    {
        var client = new Client("https://example.org/api", "some-account");

        Assert.AreEqual("some-account", client.AccountName);
        Assert.AreEqual("https://example.org/api/", client.ApplianceUri.ToString());
        Assert.IsNull(client.Authenticator);
        CollectionAssert.IsEmpty(client.TrustedCertificates);
    }

    [Test]
    public void TestConstructor_WithNullClientCertificate_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => new Client("https://example.org/api", "some-account", null));
    }

    [Test]
    public void TestConstructor_WithClientCertificate_DoesNotThrow()
    {
        using var cert = CreateSelfSignedCertificate();
        Client client = null;
        Assert.DoesNotThrow(() => client = new Client("https://example.org/api", "some-account", cert));
        Assert.AreEqual("some-account", client.AccountName);
    }

    [Test]
    public void TestConstructor_WithHttpClient()
    {
        using var httpClient = new HttpClient();
        var client = new Client(httpClient, "https://example.org/api", "some-account");

        Assert.AreEqual("some-account", client.AccountName);
        Assert.AreEqual("https://example.org/api/", client.ApplianceUri.ToString());
        Assert.AreSame(httpClient, client.httpClient);
        CollectionAssert.IsEmpty(client.TrustedCertificates);
    }

    [Test]
    public void ActingAsConstructor_CopiesAccountAndAuthenticatorFromOriginal()
    {
        Client.Authenticator = new MockAuthenticator();

        var actingAsClient = Client.ActingAs("somerole");

        Assert.AreEqual(Client.AccountName, actingAsClient.AccountName);
        Assert.AreEqual(Client.ApplianceUri, actingAsClient.ApplianceUri);
        Assert.AreSame(Client.Authenticator, actingAsClient.Authenticator);
    }

    [Test]
    public void TestCredentialSetter_InitializesAuthenticator()
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/{TestAccount}/bob/authenticate"), "the-token")
            .Verifier = (HttpRequestMessage requestMessage) =>
        {
            Assert.AreEqual(HttpMethod.Post, requestMessage.Method);
            Assert.AreEqual("some-api-key", requestMessage.Content.ReadAsStringAsync().Result);
        };

        Client.Credential = new NetworkCredential("bob", "some-api-key");

        Assert.IsInstanceOf<ApiKeyAuthenticator>(Client.Authenticator);
        Assert.AreEqual("the-token", Client.Authenticator.GetToken());
    }

    [Test]
    public void TestLogIn_NetworkCredentialOverload()
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/{TestAccount}/login"), "an-api-key").Verifier =
            (HttpRequestMessage requestMessage) => Assert.AreEqual("Basic am9objpwOmFzcw==", requestMessage.Headers.GetValues("Authorization").SingleOrDefault());

        var apiKey = Client.LogIn(new NetworkCredential("john", "p:ass"));

        Assert.AreEqual("an-api-key", apiKey);
        Assert.IsInstanceOf<ApiKeyAuthenticator>(Client.Authenticator);
    }

    [Test]
    public async Task TestLogInAsync_NetworkCredentialOverload()
    {
        Mocker.Mock(new Uri($"{BaseUri}authn/{TestAccount}/login"), "an-api-key").Verifier =
            (HttpRequestMessage requestMessage) => Assert.AreEqual("Basic am9objpwOmFzcw==", requestMessage.Headers.GetValues("Authorization").SingleOrDefault());

        var apiKey = await Client.LogInAsync(new NetworkCredential("john", "p:ass"), CancellationToken.None);

        Assert.AreEqual("an-api-key", apiKey);
        Assert.IsInstanceOf<ApiKeyAuthenticator>(Client.Authenticator);
    }

    [Test]
    public void TestRequest_BuildsUnauthenticatedRequest()
    {
        var request = Client.Request("some/path");

        Assert.AreEqual(new Uri($"{BaseUri}some/path"), request.RequestUri);
        Assert.IsFalse(request.Headers.Contains("Authorization"));
    }

    [Test]
    public void TestAuthenticatedRequest_AppendsActingAsWithQuestionMarkWhenNoExistingQuery()
    {
        Client.Authenticator = new MockAuthenticator();
        var actingAsClient = Client.ActingAs("somerole");

        var request = actingAsClient.AuthenticatedRequest("resources");

        Assert.AreEqual(new Uri($"{BaseUri}resources?acting_as=somerole"), request.RequestUri);
    }

    [Test]
    public void TestAuthenticatedRequest_AppendsActingAsWithAmpersandWhenQueryAlreadyPresent()
    {
        Client.Authenticator = new MockAuthenticator();
        var actingAsClient = Client.ActingAs("somerole");

        var request = actingAsClient.AuthenticatedRequest("resources?type=variable");

        Assert.AreEqual(new Uri($"{BaseUri}resources?type=variable&acting_as=somerole"), request.RequestUri);
    }

    [Test]
    public async Task TestAuthenticatedRequestAsync_AppendsActingAs()
    {
        Client.Authenticator = new MockAuthenticator();
        var actingAsClient = Client.ActingAs("somerole");

        var request = await actingAsClient.AuthenticatedRequestAsync("resources", CancellationToken.None);

        Assert.AreEqual(new Uri($"{BaseUri}resources?acting_as=somerole"), request.RequestUri);
    }

    [Test]
    public void TestSend_DelegatesToHttpClient()
    {
        Mocker.Mock(new Uri($"{BaseUri}some/path"), "the-content");

        var response = Client.Send(Client.Request("some/path"));

        Assert.AreEqual("the-content", response.Read());
    }

    [Test]
    public async Task TestSendAsync_DelegatesToHttpClient()
    {
        Mocker.Mock(new Uri($"{BaseUri}some/path"), "the-content");

        var response = await Client.SendAsync(Client.Request("some/path"), CancellationToken.None);

        Assert.AreEqual("the-content", await response.Content.ReadAsStringAsync());
    }

    [Test]
    public void TestValidateCertificate_DisableCertCheck_AlwaysTrue()
    {
        Client.DisableCertCheck();
        try
        {
            Assert.IsTrue(InvokeValidateCertificate(Client, SslPolicyErrors.RemoteCertificateNameMismatch));
            Assert.IsTrue(InvokeValidateCertificate(Client, SslPolicyErrors.RemoteCertificateChainErrors));
            Assert.IsTrue(InvokeValidateCertificate(Client, SslPolicyErrors.None));
        }
        finally
        {
            Client.EnableCertCheck();
        }
    }

    [Test]
    public void TestValidateCertificate_NoErrors_ReturnsTrue()
    {
        Assert.IsTrue(InvokeValidateCertificate(Client, SslPolicyErrors.None));
    }

    [Test]
    public void TestValidateCertificate_NameMismatch_ReturnsFalse()
    {
        Assert.IsFalse(InvokeValidateCertificate(Client, SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    [Test]
    public void TestValidateCertificate_ChainErrorsWithoutTrustedRoot_ReturnsFalse()
    {
        var chain = new X509Chain();

        Assert.IsFalse(InvokeValidateCertificate(Client, SslPolicyErrors.RemoteCertificateChainErrors, Certificates.SignedBySelfSigned, chain));
    }

    [Test]
    public void TestValidateCertificate_ChainErrorsWithTrustedRoot_ReturnsTrue()
    {
        var chain = new X509Chain { ChainPolicy = { RevocationMode = X509RevocationMode.NoCheck } };
        Client.TrustedCertificates.Add(Certificates.SelfSigned);
        try
        {
            Assert.IsTrue(InvokeValidateCertificate(Client, SslPolicyErrors.RemoteCertificateChainErrors, Certificates.SignedBySelfSigned, chain));
        }
        finally
        {
            Client.TrustedCertificates.Remove(Certificates.SelfSigned);
        }
    }

    [Test]
    public void TestLoadClientCertificateFromFiles_NullCertPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromFiles(null));
    }

    [Test]
    public void TestLoadClientCertificateFromFiles_EmptyCertPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromFiles(string.Empty));
    }

    [Test]
    public void TestLoadClientCertificateFromFiles_NullCertPath_WithKeyPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromFiles(null, "some/key/path"));
    }

    [Test]
    public void TestLoadClientCertificateFromFiles_EmptyCertPath_WithKeyPath_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromFiles(string.Empty, "some/key/path"));
    }

    [Test]
    public void TestLoadClientCertificateFromFiles_ValidCertAndKey_ReturnsCertificateWithPrivateKey()
    {
        var (certPem, keyPem) = GenerateSelfSignedCertPem();
        var certPath = Path.GetTempFileName();
        var keyPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(certPath, certPem);
            File.WriteAllText(keyPath, keyPem);

            using var cert = Client.LoadClientCertificateFromFiles(certPath, keyPath);

            Assert.IsTrue(cert.HasPrivateKey);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
        }
    }

    [Test]
    public void TestLoadClientCertificateFromPem_ByteArray_NullOrEmpty_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromPem((byte[])null));
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromPem(Array.Empty<byte>()));
    }

    [Test]
    public void TestLoadClientCertificateFromPem_String_NullOrEmpty_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromPem((string)null));
        Assert.Throws<ArgumentNullException>(() => Client.LoadClientCertificateFromPem(string.Empty));
    }

    [Test]
    public void TestLoadClientCertificateFromPem_ValidCertAndKeyStrings_ReturnsCertificateWithPrivateKey()
    {
        var (certPem, keyPem) = GenerateSelfSignedCertPem();

        using var cert = Client.LoadClientCertificateFromPem(certPem, keyPem);

        Assert.IsTrue(cert.HasPrivateKey);
    }

    [Test]
    public void TestLoadClientCertificateFromPem_ValidCertAndKeyBytes_ReturnsCertificateWithPrivateKey()
    {
        var (certPem, keyPem) = GenerateSelfSignedCertPem();

        using var cert = Client.LoadClientCertificateFromPem(Encoding.UTF8.GetBytes(certPem), Encoding.UTF8.GetBytes(keyPem));

        Assert.IsTrue(cert.HasPrivateKey);
    }

    [Test]
    public void TestLoadClientCertificateFromPem_CertOnly_ReturnsCertificateWithoutPrivateKey()
    {
        var (certPem, _) = GenerateSelfSignedCertPem();

        using var cert = Client.LoadClientCertificateFromPem(certPem);

        Assert.IsFalse(cert.HasPrivateKey);
    }

    [Test]
    public void TestLoadClientCertificateFromPem_InvalidPemData_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Client.LoadClientCertificateFromPem("not a real pem"));
        Assert.IsTrue(ex.InnerException is CryptographicException or ArgumentException);
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=test-client", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static (string CertPem, string KeyPem) GenerateSelfSignedCertPem()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=test-client", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var certPem = PemEncoding.WriteString("CERTIFICATE", cert.Export(X509ContentType.Cert));
        var keyPem = PemEncoding.WriteString("PRIVATE KEY", rsa.ExportPkcs8PrivateKey());

        return (certPem, keyPem);
    }

    private static bool InvokeValidateCertificate(Client client, SslPolicyErrors sslPolicyErrors, X509Certificate2 certificate = null, X509Chain chain = null)
    {
        var method = typeof(Client).GetMethod("ValidateCertificate", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)method.Invoke(client, [null, certificate ?? Certificates.SelfSigned, chain ?? new X509Chain(), sslPolicyErrors]);
    }
}
