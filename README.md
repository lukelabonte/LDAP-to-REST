# LDAP-to-REST

A cross-platform REST API that wraps Active Directory LDAP operations (user/group CRUD + membership management), deployable via Docker.

## Overview

LDAP-to-REST lets you query and manage Active Directory over plain HTTP instead of speaking the LDAP protocol directly. Point it at your AD server, and any tool that can make HTTP requests (scripts, frontends, other services) can look up users, manage group membership, and modify attributes — all through a familiar REST interface with JSON responses.

There's no service account to configure. Every API request uses the caller's own AD credentials via Basic Auth, so permissions are enforced by AD itself. Run it as a Docker container in front of your domain controller and you're done.

## Features

- **User management** — Find, view, and modify AD user attributes
- **Group management** — Find, view, and modify AD group attributes
- **Membership management** — List members, check membership, add/remove members
- **Recursive group queries** — Uses AD's matching-rule-in-chain for nested group resolution
- **Pass-through authentication** — Each request authenticates to AD with the caller's own credentials via Basic Auth (no service account needed)
- **LDAP injection prevention** — Three-layer defense: input validation, filter encoding (RFC 4515), DN encoding (RFC 4514)
- **Cross-platform** — Runs on Linux, macOS, and Windows via `System.DirectoryServices.Protocols`
- **Docker-ready** — Multi-stage Dockerfile with non-root user
- **Swagger/OpenAPI** — Interactive API docs at `/swagger`

## Quick Start

```bash
# 1. Copy the example env file and fill in your AD details
cp .env.example .env

# 2. Build and run
docker compose up --build
```

## Configuration

All configuration is via environment variables — no config files needed.

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `LDAP_HOST` | Yes | — | AD server hostname |
| `LDAP_PORT` | No | 636 (SSL) / 389 (no SSL) | LDAP port |
| `LDAP_BASE_DN` | Yes | — | Base DN for searches (e.g., `DC=example,DC=com`) |
| `LDAP_USE_SSL` | No | `false` | Enable LDAPS (port 636) |
| `LDAP_START_TLS` | No | `false` | Upgrade to TLS on port 389 before binding (ignored if `LDAP_USE_SSL=true`) |
| `LDAP_IGNORE_CERT_ERRORS` | No | `false` | Accept untrusted TLS certificates (development only) |
| `CORS_ALLOWED_ORIGINS` | No | *(disabled)* | Comma-separated allowed origins |

## API Reference

All endpoints require Basic Authentication. Credentials are passed through to AD.

### Users

#### Get user by SamAccountName
```bash
curl -u "admin:password" http://localhost:8080/api/users/jsmith
```

#### Get user by Distinguished Name
```bash
curl -u "admin:password" \
  http://localhost:8080/api/users/dn/CN=John%20Smith,OU=Users,DC=example,DC=com
```

#### Update user attributes
```bash
curl -X PATCH -u "admin:password" \
  -H "Content-Type: application/json" \
  -d '{"department": "Engineering", "title": "Senior Developer"}' \
  http://localhost:8080/api/users/jsmith
```

Modifiable attributes: `company`, `department`, `description`, `displayname`, `givenname`, `enabled`, `sn`

Setting `"enabled": false` sets the ACCOUNTDISABLE bit in `userAccountControl`. Setting a value to `null` removes the attribute.

### Groups

#### Get group by SamAccountName
```bash
curl -u "admin:password" http://localhost:8080/api/groups/developers
```

#### Get group by Distinguished Name
```bash
curl -u "admin:password" \
  http://localhost:8080/api/groups/dn/CN=Developers,OU=Groups,DC=example,DC=com
```

#### Update group attributes
```bash
curl -X PATCH -u "admin:password" \
  -H "Content-Type: application/json" \
  -d '{"description": "Development team"}' \
  http://localhost:8080/api/groups/developers
```

Modifiable attributes: `description`, `displayname`

#### List group members (paginated)
```bash
# Non-recursive (direct members only)
curl -u "admin:password" \
  "http://localhost:8080/api/groups/developers/members?page=1&pageSize=50"

# Recursive (includes nested group members)
curl -u "admin:password" \
  "http://localhost:8080/api/groups/developers/members?recursive=true"
```

#### Check membership
```bash
curl -u "admin:password" \
  http://localhost:8080/api/groups/developers/members/jsmith
```

Returns `{"isMember": true/false, "memberDistinguishedName": "...", "groupDistinguishedName": "..."}`.

#### Add member to group
```bash
curl -X POST -u "admin:password" \
  -H "Content-Type: application/json" \
  -d '{"distinguishedName": "CN=John Smith,OU=Users,DC=example,DC=com"}' \
  http://localhost:8080/api/groups/developers/members
```

#### Remove member from group
```bash
curl -X DELETE -u "admin:password" \
  http://localhost:8080/api/groups/developers/members/jsmith
```

### Health Check

```bash
curl http://localhost:8080/health
# {"status":"healthy"}
```

No authentication required.

## Authentication

Every API request must include an `Authorization: Basic <base64>` header. The API does **not** maintain a service account — it uses your credentials to bind to AD on each request. This means:

- Users can only see/modify what their AD permissions allow
- Failed credentials return `401 Unauthorized`
- Insufficient permissions return `403 Forbidden`

## Error Handling

| HTTP Status | When |
|-------------|------|
| 400 | Invalid input (bad SamAccountName, unsupported attribute, malformed request) |
| 401 | Missing/invalid credentials, or AD rejected the credentials |
| 403 | AD user lacks permission for the requested operation |
| 404 | User or group not found in AD |
| 409 | Entry already exists, or AD server unwilling to perform operation |
| 500 | Unexpected LDAP error |
| 502 | Cannot reach the AD server |

All error responses include a JSON body:
```json
{
  "status": 404,
  "message": "Object not found in directory",
  "detail": "..."
}
```

## Building from Source

```bash
# Prerequisites: .NET 8 SDK

# Build
dotnet build

# Run tests
dotnet test

# Run locally
LDAP_HOST=dc01.example.com LDAP_BASE_DN="DC=example,DC=com" \
  dotnet run --project src/LdapToRest
```

## Docker

```bash
# Copy env file and configure
cp .env.example .env
# Edit .env with your AD server details

# Build and run
docker compose up --build

# Or run directly with docker
docker run -p 8080:8080 \
  -e LDAP_HOST=dc01.example.com \
  -e LDAP_BASE_DN="DC=example,DC=com" \
  ldap-to-rest
```

## Security Considerations

- **LDAP injection prevention**: All user input is validated and encoded before being placed in LDAP filters or DN assertions. Three layers: input validation (reject invalid characters), RFC 4515 filter encoding, and RFC 4514 DN encoding.
- **No credential storage**: Credentials are never stored — they're extracted from the HTTP header and used for a single LDAP bind per request.
- **HTTPS recommended**: Basic Auth sends credentials in base64 (not encrypted). Always use HTTPS in production (terminate TLS at a reverse proxy or load balancer).
- **Non-root Docker**: The container runs as a non-root user.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test, and submit pull requests.

## Security

If you discover a security vulnerability, **do not open a public issue.** See [SECURITY.md](SECURITY.md) for how to report it privately.

## License

[MIT](LICENSE)
