# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [v1.0.0] - 2026-02-14

### Added
- REST API for Active Directory user operations: search by sAMAccountName, modify attributes, and derive account status from userAccountControl flags
- REST API for Active Directory group operations: CRUD, membership management, and recursive group membership queries
- Pass-through Basic Auth — each request binds to AD with the caller's own credentials (no service account)
- Three-layer LDAP injection prevention: input validation, RFC 4515 filter encoding, and RFC 4514 DN encoding
- Docker support with multi-stage build and .env-based configuration
- Swagger UI with Basic Auth security definition and full endpoint documentation
- Comprehensive README with API reference, curl examples, and configuration guide

### Fixed
- LDAP connection hardened for Linux and non-SSL Active Directory: explicit bind, referral chasing disabled, StartTLS support, LDAPTLS_REQCERT workaround for OpenLDAP
- objectClass now returns the most specific class (e.g., user) instead of the generic base (top)
