# Certificate Authenticator Implementation for Conjur .NET SDK

## Executive Summary

Successfully implemented certificate-based authentication (authn-cert) for the CyberArk Secrets Manager .NET SDK, mirroring the implementation pattern from the Go SDK (`conjur-api-go`). The implementation adds full mTLS support with both Request Mode and SPIFFE Mode authentication flows.

## What's New

### 1. CertAuthenticator Class
A new authenticator that enables mutual TLS (mTLS) authentication using X.509 client certificates.

**File:** `conjur-api/CertAuthenticator.cs`

Features:
- ✅ Request Mode: Client supplies Conjur host ID in URL
- ✅ SPIFFE Mode: Host derived from certificate's SPIFFE SAN URI
- ✅ Token caching with automatic expiration
- ✅ Thread-safe token acquisition using SemaphoreSlim
- ✅ Full async/await support
- ✅ Implements IAuthenticator for seamless Client integration

### 2. Client mTLS Support
Extended the Client class with certificate loading capabilities.

**File:** `conjur-api/Client.cs`

New constructor:
```csharp
public Client(string applianceUri, string account, X509Certificate2 clientCertificate)
```

New helper methods:
```csharp
// Load from PEM files
public static X509Certificate2 LoadClientCertificateFromFiles(
    string certPath, 
    string keyPath = null)

// Load from byte arrays
public static X509Certificate2 LoadClientCertificateFromPem(
    byte[] certPem, 
    byte[] keyPem = null)

// Load from strings
public static X509Certificate2 LoadClientCertificateFromPem(
    string certPem, 
    string keyPem = null)
```

### 3. Comprehensive Test Suite
**File:** `test/CertAuthenticatorTest.cs`

Five test cases covering:
- Token caching behavior
- Asynchronous token operations
- Thread-safe concurrent token acquisition
- SPIFFE mode (empty host ID)
- Request mode (with host ID)

All tests pass ✅

### 4. Documentation
- **CERT_AUTHENTICATOR.md** - Complete implementation guide
- **IMPLEMENTATION_SUMMARY.md** - Summary of what was implemented
- **example/CertAuthenticatorExample.cs** - Usage examples

## Quick Start

### Request Mode Example
```csharp
using Conjur;

// Load client certificate
var clientCert = Client.LoadClientCertificateFromFiles("client.pem", "client-key.pem");

// Create client with mTLS
var client = new Client("https://conjur.example.com", "conjur", clientCert);

// Add server CA if needed
client.TrustedCertificates.ImportPem("ca-cert.pem");

// Create authenticator with host ID
var authenticator = new CertAuthenticator(client, "my-service", "host/workloads/app-01");
client.Authenticator = authenticator;

// Authenticate
var token = authenticator.GetToken();

// Access secrets
var secret = client.Variable("data/vault/secret").GetValue();
```

### SPIFFE Mode Example
```csharp
using Conjur;

// Load certificate
var clientCertPem = File.ReadAllText("client.pem");
var clientKeyPem = File.ReadAllText("client-key.pem");
var clientCert = Client.LoadClientCertificateFromPem(clientCertPem, clientKeyPem);

// Create client
var client = new Client("https://conjur.example.com", "conjur", clientCert);
client.TrustedCertificates.ImportPem("ca-cert.pem");

// Create authenticator without host ID (SPIFFE mode)
var authenticator = new CertAuthenticator(client, "my-service", "");
client.Authenticator = authenticator;

// Authenticate - host is derived from certificate
var token = authenticator.GetToken();
```

## Implementation Details

### Authentication Flow
1. Client certificate is configured on the HttpClientHandler
2. CertAuthenticator.GetToken() is called
3. Constructs POST request to `/authn-cert/<service-id>/<account>[/<host-id>]/authenticate`
4. TLS handshake includes client certificate
5. Server validates certificate and returns Conjur token
6. Token is cached for reuse until expiration

### Token Lifecycle
- Tokens are cached on first retrieval
- Cache timeout: Configurable (default 5 minutes)
- Token is automatically invalidated after timeout
- Next call triggers new authentication

### Thread Safety
- SemaphoreSlim prevents concurrent authentication requests
- Multiple threads can safely call GetToken() simultaneously
- Only one auth request is made during token refresh
- No "thundering herd" problem

### Two Authentication Modes

#### Request Mode
- Client specifies host ID in URL
- Conjur matches certificate fields to host annotations
- URL format: `/authn-cert/service-id/account/host-id/authenticate`

#### SPIFFE Mode
- Host derived from certificate's SPIFFE SAN URI
- No host ID in URL
- Automatic host resolution
- URL format: `/authn-cert/service-id/account/authenticate`

## Build & Test

### Build
```bash
dotnet build
```

### Run Tests
```bash
# All tests
dotnet test test/test.csproj

# Only CertAuthenticator tests
dotnet test test/test.csproj --filter "CertAuthenticatorTest"
```

### Test Results
✅ 49 tests passed
❌ 0 tests failed
⊘ 1 test skipped (AWS environment required)

## Files Changed

### New Files
- ✅ `conjur-api/CertAuthenticator.cs` (122 lines)
- ✅ `test/CertAuthenticatorTest.cs` (174 lines)
- ✅ `example/CertAuthenticatorExample.cs` (86 lines)
- ✅ `CERT_AUTHENTICATOR.md` (Documentation)
- ✅ `IMPLEMENTATION_SUMMARY.md` (Summary)

### Modified Files
- ✅ `conjur-api/Client.cs` (Added mTLS support)

### No Breaking Changes
- Existing Client constructors remain unchanged
- All existing tests pass
- Backward compatible with all existing code

## Requirements

### Server-Side
- CyberArk Secrets Manager Self-Hosted v13.8+
- authn-cert authenticator enabled
- (Conjur Cloud/SaaS NOT supported)

### Client-Side
- .NET 6.0 or later
- X.509 client certificate (provisioned out-of-band)
- For Request Mode: Conjur host with matching annotations
- For SPIFFE Mode: Certificate with valid SPIFFE SAN URI

## Reference Implementation

This implementation mirrors the Go SDK (`conjur-api-go`) design:

| Component | Go | .NET | Status |
|-----------|-----|------|--------|
| CertAuthenticator | ✅ | ✅ | Complete |
| Request Mode | ✅ | ✅ | Complete |
| SPIFFE Mode | ✅ | ✅ | Complete |
| Token Caching | ✅ | ✅ | Complete |
| Thread Safety | ✅ | ✅ | Complete |
| File Loading | ✅ | ✅ | Complete |
| PEM Support | ✅ | ✅ | Complete |
| Async Support | ✅ | ✅ | Complete |
| Config Validation | ✅ | ⊘ | Future |
| File Rotation | ✅ | ⊘ | Future |

## Future Enhancements

The Go implementation includes additional phases (Phase 2+) that could be added in future iterations:

1. **Configuration-based setup** - Separate Config struct with validation
2. **Environment variable support** - CONJUR_AUTHN_CERT_* env vars
3. **File rotation** - Transparent certificate reloading
4. **Config validation** - Early error detection
5. **Debug logging** - With private key redaction

These are tracked but not included in this iteration to keep the scope focused on the core authenticator functionality.

## Key Differences from Go

1. **HTTP Handler Integration** - .NET uses HttpClientHandler directly vs Go's GetClientCertificate callback
2. **Certificate Rotation** - Go supports transparent file rotation via callback; .NET requires new Client instance
3. **Configuration** - Go has separate Config struct; .NET uses Client constructor parameters
4. **Scope** - This iteration focuses on Phase 1 (core authenticator); Go includes Phases 1-7

## Support

For issues or questions:
1. See CERT_AUTHENTICATOR.md for detailed documentation
2. Check example/CertAuthenticatorExample.cs for usage patterns
3. Review test/CertAuthenticatorTest.cs for test examples

## Conclusion

The certificate authenticator implementation for the .NET SDK is complete, tested, and ready for use. It provides full mTLS authentication support with both Request Mode and SPIFFE Mode, seamlessly integrating with the existing Conjur Client API.

