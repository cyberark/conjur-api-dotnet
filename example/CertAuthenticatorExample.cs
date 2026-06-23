// <copyright file="CertAuthenticatorExample.cs" company="CyberArk Software Ltd.">
//     Copyright (c) 2025 CyberArk Software Ltd. All rights reserved.
// </copyright>
// <summary>
//     Example showing how to use certificate-based authentication (mTLS) with the Conjur .NET SDK.
// </summary>

/*
 * Certificate Authentication Example
 * 
 * This example shows how to use certificate-based authentication (mTLS) with the Conjur .NET SDK.
 * 
 * Certificate authentication (authn-cert) allows workloads to authenticate using an X.509 client 
 * certificate over mutual TLS (mTLS). Rather than exchanging credentials in the request body, 
 * the client certificate is presented during the TLS handshake.
 * 
 * There are two server-side host resolution modes:
 * 
 * 1. Request Mode (default): The caller supplies the Conjur host ID in the URL. At least one 
 *    workload annotation must match the certificate fields.
 *    
 * 2. SPIFFE Mode: The host is derived automatically from the SPIFFE ID in the certificate's 
 *    SAN URI. Leave HostId empty to use SPIFFE mode.
 * 
 * ==================== Request Mode Example ====================
 * 
 *     using Conjur;
 *     
 *     // Load the client certificate from files
 *     var clientCert = Client.LoadClientCertificateFromFiles(
 *         "/path/to/client.pem", 
 *         "/path/to/client-key.pem"
 *     );
 *     
 *     // Create a client with the certificate for mTLS
 *     var client = new Client("https://conjur.example.com", "conjur", clientCert);
 *     
 *     // Load the server's CA certificate if using a self-signed or internal CA
 *     client.TrustedCertificates.ImportPem("/path/to/ca.pem");
 *     
 *     // Create the certificate authenticator (request mode with host ID)
 *     var authenticator = new CertAuthenticator(
 *         client, 
 *         "my-service",  // service ID
 *         "host/vm-workloads/vm-01"  // host ID for request mode
 *     );
 *     client.Authenticator = authenticator;
 *     
 *     // Authenticate (this will use mTLS)
 *     var token = authenticator.GetToken();
 *     Console.WriteLine("Authenticated successfully!");
 *     
 *     // Now you can use the authenticated client to access Conjur resources
 *     var variable = client.Variable("data/vault/myapp/password");
 *     var secretValue = variable.GetValue();
 * 
 * 
 * ==================== SPIFFE Mode Example ====================
 * 
 *     using Conjur;
 *     
 *     // Load the client certificate from inline PEM strings
 *     var clientCertPem = File.ReadAllText("/path/to/client.pem");
 *     var clientKeyPem = File.ReadAllText("/path/to/client-key.pem");
 *     
 *     var clientCert = Client.LoadClientCertificateFromPem(clientCertPem, clientKeyPem);
 *     
 *     // Create a client with the certificate for mTLS
 *     var client = new Client("https://conjur.example.com", "conjur", clientCert);
 *     
 *     // Load the server's CA certificate
 *     client.TrustedCertificates.ImportPem("/path/to/ca.pem");
 *     
 *     // For SPIFFE mode, leave hostId empty
 *     var authenticator = new CertAuthenticator(
 *         client, 
 *         "my-service",  // service ID
 *         ""  // empty host ID for SPIFFE mode
 *     );
 *     client.Authenticator = authenticator;
 *     
 *     // Authenticate - host is derived from certificate's SPIFFE SAN URI
 *     var token = authenticator.GetToken();
 *     Console.WriteLine("Authenticated via SPIFFE successfully!");
 * 
 * 
 * ==================== Requirements ====================
 * 
 * - CyberArk Secrets Manager, Self-Hosted v13.8 or later (Conjur OSS was not tested)
 * - The authn-cert authenticator must be enabled on the Conjur server
 * - The client certificate must be provisioned out-of-band (e.g., by an internal PKI or SPIFFE/SPIRE)
 * - Conjur host must have matching workload annotations or SPIFFE identity
 * - This authenticator is NOT supported in Conjur Cloud/SaaS
 * 
 */
namespace Example;

// This class serves as documentation. The actual usage examples are in the comments above.
// For usage, refer to the comments in this file.


