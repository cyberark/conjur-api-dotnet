# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.3.0] - 2026-08-04

### Added
- `AzureAuthenticator` for authenticating via Azure IMDS, supporting both
  system-assigned and user-assigned managed identities. (CNJR-13672)
- `GCPAuthenticator` for authenticating via the GCP instance metadata identity
  token, with `RefreshJwtAsync` for token renewal. (CNJR-14035)
- `CertAuthenticator` for certificate-based (mTLS) authentication, plus a new
  `Client(string applianceUri, string account, X509Certificate2 clientCertificate)`
  constructor and the `Client.LoadClientCertificateFromFiles` and
  `Client.LoadClientCertificateFromPem` helpers for loading PEM certificate and
  key material. (CNJR-13931)

### Changed
- The Release stage no longer runs a repo-level Artifactory push. Artifactory
  publishing is now handled entirely by the shared release library; the RELEASE
  stage only copies build artifacts for it to upload. (CNJR-14561)

### Fixed
- `Policy.LoadPolicy` and `LoadPolicyAsync` now rewind the buffered policy stream
  before sending, so policy content is no longer transmitted as an empty body.
- Failed policy loads now raise an `HttpRequestException` that includes the
  status code, path, and Conjur response body instead of the bare status
  assertion from `EnsureSuccessStatusCode`.

## [3.2.3] - 2025-12-15

### Changed
- Handle 404 response code for privilge check on resource (CNJR-12022)

## [3.2.2] - 2025-11-24

## [3.2.1] - 2025-10-22

### Changed
- Improved release process to push package to nuget.org
  ([cyberark/conjur-api-dotnet#17](https://github.com/cyberark/conjur-api-dotnet/issues/17), CNJR-9543)

## [3.2.0] - 2025-10-07

### Added
- Support for JWT authentication (CNJR-11283)

## [3.1.1] - 2025-09-17

### Changed
- Updated documentation to align with Conjur Enterprise name change to Secrets Manager. (CNJR-10974)

## [3.1.0] - 2025-09-03

### Added
- Target .NET 8.0
- Add Async methods
  ([cyberark/conjur-api-dotnet#92](https://github.com/cyberark/conjur-api-dotnet/issues/92)), CNJR-9409

## [3.0.4] - 2025-01-17

### Fixed
- Updated CLI command in documentation
  ([cyberark/conjur-api-dotnet#53](https://github.com/cyberark/conjur-api-dotnet/issues/53))
- Added .NET version requirements to README
  ([cyberark/conjur-api-dotnet#62](https://github.com/cyberark/conjur-api-dotnet/issues/62))
- Added code coverage reporting
  ([cyberark/conjur-api-dotnet#35](https://github.com/cyberark/conjur-api-dotnet/issues/35))

## [3.0.3] - 2024-11-22

### Fixed
- Resolve build warnings
- Fix and re-enable broken tests (CNJR-7227)

## [3.0.2] - 2024-08-02

### Changed
- Automated release process

## [3.0.1] - 2024-02-27
### Fixed
- Fix broken API Key authentication

## [3.0.0] - 2024-02-14
### Changed
- Target .NET 6+. If you're using .NET Framework, continue using v2.1.1.
  [cyberark/conjur-api-dotnet#89](https://github.com/cyberark/conjur-api-dotnet/pull/89)
- Support for AWS authentication
  [cyberark/conjur-api-dotnet#90](https://github.com/cyberark/conjur-api-dotnet/pull/90)

## [2.1.1] - 2022-03-14
### Fixed
- Fix mime type "text/plain"
  [cyberark/conjur-api-dotnet#82](https://github.com/cyberark/conjur-api-dotnet/pull/82)

## [2.1.0] - 2021-09-08
### Added
- Add parameter to the function `Policy::LoadPolicy()` to allow a different load method other than POST. POST being the default value. Currently Conjur supports POST, PUT and PATCH

## [2.0.0] - 2020-06-10
### Added
- Conjur V5 API support ([cyberark/conjur-api-dotnet#43](https://github.com/cyberark/conjur-api-dotnet/issues/43))
- Ability to control "limit" and "offset" for `ListUsers()` and `ListVariables()`

### Removed
- Conjur V4 API support (see [v4 branch](https://github.com/cyberark/conjur-api-dotnet/tree/v4)
  for v4 API support)

## [1.4.0] - 2018-07-05
### Added
- Add Role entity with all corresponding methods

## 1.3.0 - 2018-05-22
### Added
- User entity
- Client.ListUsers method to list for users

## 1.2.0 - 2018-04-25
### Added
- Client.ListVariables method to list for variables.
- Client.ActingAs property (currently with support limited to the above).

## [1.1.1] - 2018-03-06
### Fixed
- The built-in authenticator is now thread-safe.

## 1.1.0 - 2018-01-09
### Added
- `Variable.AddValue()` method for adding variable values.

[Unreleased]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.1.1...HEAD
[3.1.1]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.1.0...v3.1.1
[3.1.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.0.4...v3.1.0
[3.0.4]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.0.3...v3.0.4
[3.0.3]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.0.2...v3.0.3
[3.0.2]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.0.1...v3.0.2
[3.0.1]: https://github.com/cyberark/conjur-api-dotnet/compare/v3.0.0...v3.0.1
[3.0.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v2.1.1...v3.0.0
[2.1.1]: https://github.com/cyberark/conjur-api-dotnet/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.4.0...v2.0.0
[1.4.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.3.0...v1.4.0
[1.1.1]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/cyberark/conjur-api-dotnet/releases/tag/v1.1.0
