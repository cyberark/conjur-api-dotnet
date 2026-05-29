# Certificate Authenticator Implementation for .NET SDK

## Overview

This implementation adds support for certificate-based authentication (authn-cert) to the CyberArk Secrets Manager .NET SDK. This allows workloads to authenticate using X.509 client certificates over mutual TLS (mTLS).

## What was implemented

### 1. CertAuthenticator Class (`conjur-api/CertAuthenticator.cs`)

A new authenticator class that implements the `IAuthenticator` interface for certificate-based authentication.

**Key Features:**
- Supports both **Request Mode** and **SPIFFE Mode**
- Request Mode: Client supplies the Conjur host ID in the URL
- SPIFFE Mode: Host is derived from the certificate's SPIFFE SAN URI (leave HostId empty)
- Token caching with automatic expiration
- Thread-safe implementation using SemaphoreSlim
- Support for both synchronous and asynchronous token retrieval

**Constructor:**
```csharp
public CertAuthenticator(Client client, string serviceId, string hostId = "")
```

- `client`: The Conjur client instance
- `serviceId`: The service ID for the cert authenticator (e.g., "my-service")
- `hostId`: The Conjur host ID for request mode (leave empty for SPIFFE mode)

### 2. Client Extensions (`conjur-api/Client.cs`)

Added support for mTLS client certificates in the Client class.

**New Constructor:**
```csharp
public Client(string applianceUri, string account, X509Certificate2 clientCertificate)
```

Allows creation of a Client with an mTLS certificate that will be used for all HTTPS connections.

**New Static Helper Methods:**

- `LoadClientCertificateFromFiles(string certPath, string keyPath = null)`
  - Loads a client certificate from PEM files
  - Supports separate cert and key files or combined PEM file

- `LoadClientCertificateFromPem(byte[] certPem, byte[] keyPem = null)`
  - Loads a client certificate from PEM byte arrays

- `LoadClientCertificateFromPem(string certPem, string keyPem = null)`
  - Loads a client certificate from PEM strings (useful for inline PEM data)

### 3. Unit Tests (`test/CertAuthenticatorTest.cs`)

Comprehensive test coverage including:
- Token caching behavior
- Async token retrieval
- Thread-safety of token acquisition
- SPIFFE mode (empty host ID)
- Request mode (with host ID)
- Token expiration and refresh

## Usage Examples

### Request Mode (with Host ID)

```csharp
using Conjur;

// Load the client certificate from files
var clientCert = Client.LoadClientCertificateFromFiles(
    "/path/to/client.pem", 
    "/path/to/client-key.pem"
);

// Create a client with the certificate for mTLS
var client = new Client("https://conjur.example.com", "conjur", clientCert);

// Load the server's CA certificate if using a self-signed or internal CA
client.TrustedCertificates.ImportPem("/path/to/ca.pem");

// Create the certificate authenticator (request mode with host ID)
var authenticator = new CertAuthenticator(
    client, 
    "my-service",                    // service ID
    "host/vm-workloads/vm-01"        // host ID for request mode
);
client.Authenticator = authenticator;

// Authenticate (this will use mTLS)
var token = authenticator.GetToken();

// Now you can use the authenticated client to access Conjur resources
var variable = client.Variable("data/vault/myapp/password");
var secretValue = variable.GetValue();
```

### SPIFFE Mode (empty Host ID)

```csharp
using Conjur;

// Load the client certificate from inline PEM strings
var clientCertPem = File.ReadAllText("/path/to/client.pem");
var clientKeyPem = File.ReadAllText("/path/to/client-key.pem");

var clientCert = Client.LoadClientCertificateFromPem(clientCertPem, clientKeyPem);

// Create a client with the certificate for mTLS
var client = new Client("https://conjur.example.com", "conjur", clientCert);

// Load the server's CA certificate
client.TrustedCertificates.ImportPem("/path/to/ca.pem");

// For SPIFFE mode, leave hostId empty
var authenticator = new CertAuthenticator(
    client, 
    "my-service",    // service ID
    ""               // empty host ID for SPIFFE mode
);
client.Authenticator = authenticator;

// Authenticate - host is derived from certificate's SPIFFE SAN URI
var token = authenticator.GetToken();
```

## Implementation Details

### How mTLS Works

When the Client is created with a certificate parameter:
1. The certificate is added to the HttpClientHandler's `ClientCertificates` collection
2. During TLS handshake, the certificate is presented to the server
3. Server validates the certificate and the client identity is established at the TLS layer
4. No `Authorization: Token` header is needed (identity is established via mTLS)

### URI Format

- **Request Mode:** `POST /authn-cert/<service-id>/<account>/<url-encoded-host-id>/authenticate`
- **SPIFFE Mode:** `POST /authn-cert/<service-id>/<account>/authenticate`

### Token Lifecycle

1. Token is fetched on first call to `GetToken()` or `GetTokenAsync()`
2. Token is cached for reuse
3. After token expiration timeout (default 5 minutes), the token is invalidated
4. Next call to `GetToken()` or `GetTokenAsync()` will fetch a new token

### Thread Safety

- Token acquisition is protected by a `SemaphoreSlim` to prevent thundering herd during token refresh
- Multiple threads can safely call `GetToken()` or `GetTokenAsync()` simultaneously
- Only one authentication request is made, even when multiple threads request tokens simultaneously

## Compatibility

- **Target Framework:** .NET 6.0 or later (.NET 8.0 currently)
- **Conjur Version:** CyberArk Secrets Manager Self-Hosted (DAP/Enterprise) v13.8+
- **Not Supported:** Conjur OSS, Conjur Cloud/SaaS

> ⚠️ **Important:** `authn-cert` is a **CyberArk Secrets Manager Enterprise (DAP) only** feature.
> It is **not available** in Conjur Open Source or Conjur Cloud.
> The Conjur Enterprise appliance must have `authn-cert` listed in `CONJUR_AUTHENTICATORS`
> and the authenticator must be configured before use.

## Requirements

1. CyberArk Secrets Manager Enterprise (DAP) appliance — Conjur OSS does **not** support `authn-cert`
2. The `authn-cert/<service-id>` authenticator must be enabled on the Conjur server
3. Client certificates must be provisioned out-of-band (e.g., by internal PKI or SPIFFE/SPIRE)
4. For Request Mode: Conjur host must have matching workload annotations
5. For SPIFFE Mode: Certificate must contain valid SPIFFE SAN URI

## Testing

Run the unit tests:
```bash
dotnet test test/test.csproj --filter "CertAuthenticatorTest"
```

All tests pass successfully:
- Token caching and expiration
- Thread-safety
- SPIFFE mode
- Request mode

## Files Modified/Created

1. **Created:** `conjur-api/CertAuthenticator.cs` (122 lines)
   - Main certificate authenticator implementation

2. **Modified:** `conjur-api/Client.cs`
   - Added new constructor with certificate support
   - Added helper methods to load certificates from files and PEM

3. **Created:** `test/CertAuthenticatorTest.cs` (174 lines)
   - Comprehensive unit tests for CertAuthenticator

4. **Created:** `example/CertAuthenticatorExample.cs` (86 lines)
   - Documentation and usage examples

## Differences from Go Implementation

While the overall design matches the Go SDK's cert authenticator, there are some .NET-specific differences:

1. **Certificate Loading:** .NET uses `X509Certificate2.CreateFromPem()` instead of Go's `tls.X509KeyPair()`
2. **HTTP Transport:** .NET uses `HttpClientHandler.ClientCertificates` instead of Go's `GetClientCertificate` callback
3. **No explicit file rotation support:** In .NET, certificate rotation would require creating a new Client instance, while Go supports transparent rotation via callback
4. **Configuration:** The .NET implementation uses a Client constructor parameter rather than a separate Config struct

## Future Enhancements

1. Configuration-based setup (similar to Go's `NewClientFromCertificate`)
2. Transparent certificate file rotation support
3. Environment variable configuration
4. Configuration validation (require ServiceID, ClientCert, etc.)
5. Private key redaction in debug logging

These enhancements align with the full Go implementation plan but are left for future iterations.

