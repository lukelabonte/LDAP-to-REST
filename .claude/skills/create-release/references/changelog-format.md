# Changelog Format Reference

Based on [Keep a Changelog](https://keepachangelog.com/).

## Version Entry Format

```
## [vX.Y.Z] - YYYY-MM-DD

### Added
- New features or capabilities

### Fixed
- Bug fixes

### Changed
- Changes to existing functionality

### Removed
- Removed features or capabilities

### Security
- Security-related changes or vulnerability fixes

### Deprecated
- Features that will be removed in a future version
```

## Rules

1. **Date format:** `YYYY-MM-DD` (ISO 8601)
2. **Version format:** `## [vX.Y.Z] - YYYY-MM-DD`
3. **Categories:** Only include categories that have entries. Order: Added, Fixed, Changed, Removed, Security, Deprecated
4. **Unreleased section:** Always keep `## [Unreleased]` at the top. New changes go here until a release is cut
5. **Most recent first:** Newest version entries appear at the top, just below `[Unreleased]`
6. **Each entry:** Start with `- ` (dash + space), written in imperative mood ("Add feature" not "Added feature")
7. **Be concise:** Focus on what changed from the user's perspective, not implementation details

## Semantic Versioning

- **Major (X.0.0):** Breaking changes — API contract changes, removed endpoints, incompatible config changes
- **Minor (0.X.0):** New features — new endpoints, new optional config, new query parameters
- **Patch (0.0.X):** Bug fixes — corrected behavior, security patches, documentation fixes

## Example

```markdown
## [Unreleased]

## [v1.1.0] - 2026-03-15

### Added
- Recursive group membership queries via `?recursive=true` parameter
- Health check endpoint at `/health`

### Fixed
- objectClass returning "top" instead of most specific class

### Changed
- Swagger documentation now includes all response codes

## [v1.0.0] - 2026-02-14

### Added
- User CRUD operations (get by SamAccountName, get by DN, update attributes)
- Group CRUD operations (get by SamAccountName, get by DN, update attributes)
- Membership management (list, check, add, remove)
- Pass-through Basic Auth (no service account)
- LDAP injection prevention (input validation + RFC 4515 + RFC 4514)
- Docker support with non-root user
- Swagger/OpenAPI documentation
```
