---
name: Bug Report
about: Something isn't working as expected
title: ""
labels: bug
assignees: ""
---

**Describe the bug**
A clear description of what's going wrong.

**To reproduce**
Steps or curl command to reproduce:
```bash
curl -u "user:pass" http://localhost:8080/api/...
```

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened. Include the full error response JSON if available:
```json

```

**Environment**
- Running via: Docker / bare metal
- OS: e.g., Ubuntu 22.04, Windows Server 2022
- .NET version (if bare metal): `dotnet --version`
- LDAP-to-REST version/commit:

**LDAP configuration** (redact hostnames if needed)
- `LDAP_HOST`:
- `LDAP_PORT`:
- `LDAP_USE_SSL`:
- `LDAP_START_TLS`:
- `LDAP_IGNORE_CERT_ERRORS`:

**Docker logs** (if applicable)
```
docker compose logs ldap-to-rest
```

**Additional context**
Any other details — AD version, network setup, etc.
