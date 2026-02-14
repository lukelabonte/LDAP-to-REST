# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in LDAP-to-REST, **please do not open a public issue.**

Instead, use [GitHub's private vulnerability reporting](https://github.com/lukelabonte/LDAP-to-REST/security/advisories/new) to report it confidentially.

Include:
- A description of the vulnerability
- Steps to reproduce
- Potential impact

You should receive a response within 48 hours.

## Security Considerations

This project handles Active Directory credentials on every request. Please keep in mind:

- **Always use HTTPS in production** — Basic Auth transmits credentials in base64 (not encrypted). Terminate TLS at a reverse proxy or load balancer.
- **No credential storage** — Credentials are extracted from the HTTP header and used for a single LDAP bind per request. They are never logged or persisted.
- **LDAP injection prevention** — All user input is validated and encoded before being placed in LDAP filters or DN assertions.
- **Non-root Docker** — The container runs as a non-root user.
