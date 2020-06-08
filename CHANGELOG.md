# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Conjur V5 API support
- Ability to control "limit" and "offset" for `ListUsers()` and `ListVariables()`

### Removed
- Conjur V4 API support

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

## [1.1.0] - 2018-01-09
### Added
- `Variable.AddValue()` method for adding variable values.

[Unreleased]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.3.0...v1.4.0
[1.1.1]: https://github.com/cyberark/conjur-api-dotnet/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/cyberark/conjur-api-dotnet/releases/tag/v1.1.0
