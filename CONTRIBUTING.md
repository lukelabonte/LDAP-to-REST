# Contributing to LDAP-to-REST

Thanks for your interest in contributing!

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) (for building/testing the container)

## Getting Started

```bash
# Clone the repo
git clone https://github.com/lukelabonte/LDAP-to-REST.git
cd LDAP-to-REST

# Build
dotnet build

# Run tests
dotnet test

# Verify Docker build
docker build -t ldap-to-rest .
```

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b my-feature`)
3. Make your changes
4. Run `dotnet test` and make sure all tests pass
5. Run `docker build -t ldap-to-rest .` to verify the Docker build
6. Commit with a clear message describing what and why
7. Open a pull request

## Code Style

- The project uses an `.editorconfig` — most editors will pick this up automatically
- 4 spaces for indentation
- Follow existing patterns in the codebase

## Testing

Tests use xUnit and Moq. LDAP operations are tested against mocked `ILdapConnection` — no real AD server is needed to run the test suite.

When adding a new endpoint or service method, include unit tests that cover the expected behavior.

## Project Structure

```
src/LdapToRest/          # Main API project
  Controllers/            # REST endpoints
  Services/               # LDAP operations (search, modify, membership)
  Middleware/              # Auth handler, exception mapping
  Models/                 # DTOs and request/response types
  Configuration/          # Settings classes

tests/LdapToRest.Tests/   # Unit tests (mirrors src/ structure)
```
