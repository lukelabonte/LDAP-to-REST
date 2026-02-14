# LDAP-to-REST

## Build & Test
- .NET 8 SDK (keg-only on macOS): `PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"` required before `dotnet` commands
- Build: `dotnet build`
- Test: `dotnet test` (83 tests, xUnit + Moq)
- Docker: `docker build -t ldap-to-rest .`

## Project Structure
- `src/LdapToRest/` — ASP.NET Core 8 web API
- `tests/LdapToRest.Tests/` — Unit tests (mock `ILdapConnection`, not real LDAP)
- Swashbuckle pinned to `6.9.*` (10.x incompatible with .NET 8's `Microsoft.OpenApi.Models`)
- `Microsoft.AspNetCore.Mvc.Testing` pinned to `8.0.*` (latest targets .NET 10)
- Swagger docs sourced from XML doc comments (`/// <summary>`) — requires `GenerateDocumentationFile` in csproj + `IncludeXmlComments()` in Program.cs

## GitHub
- Releases: push a `v*` tag → CI tests, builds Docker image, pushes to ghcr.io, creates GitHub Release
- Issue templates in `.github/ISSUE_TEMPLATE/` — blank issues disabled

## When Changing Endpoints or Attributes
When adding, removing, or modifying any API endpoint or modifiable attribute:
1. Update XML doc comments on the controller action (`/// <summary>`, `/// <param>`, `/// <response>`, `[ProducesResponseType]`)
2. Update the README API Reference section with curl examples
3. Update any affected DTO model doc comments
4. If adding/changing env vars, update the README Configuration table and `.env.example`
5. Verify Swagger renders correctly at `/swagger` after changes

## Architecture
- Pass-through Basic Auth: each request binds to AD with the caller's own credentials (no service account)
- `ILdapConnection`/`LdapEntry`/`LdapConnectionAdapter` wraps `System.DirectoryServices.Protocols` because `SearchResponse`/`SearchResultEntry` are sealed and unmockable
- Three-layer LDAP injection prevention: `InputValidator` → `LdapFilterEncoder` (RFC 4515) → `LdapDnEncoder` (RFC 4514)
- `ExceptionHandlingMiddleware` catches both `LdapException` AND `DirectoryOperationException` (sibling classes, not parent-child)

## LDAP Gotchas (discovered in production debugging)
- `objectClass` is multi-valued in hierarchy order (`top`, `person`, ..., `user`) — always use `LastOrDefault()` for the most specific class
- On Linux, `System.DirectoryServices.Protocols` uses OpenLDAP's `libldap` which ignores .NET's `VerifyServerCertificate` callback for StartTLS — set `LDAPTLS_REQCERT=never` env var instead
- Use 3-param `LdapConnection` constructor: `new LdapConnection(identifier, credential, AuthType.Basic)`
- Disable referral chasing (`ReferralChasingOptions.None`) to prevent unauthenticated follow-on connections
- Set `AutoBind = false` and call `Bind(credential)` explicitly
- `LDAP_START_TLS` defaults to `false` — not all AD servers support it
