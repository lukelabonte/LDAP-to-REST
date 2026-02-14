namespace LdapToRest.Services;

/// <summary>
/// Encodes distinguished names for use in LDAP filter assertions.
/// Layer 3 of 3-layer injection prevention.
/// When a DN is placed inside an LDAP filter (e.g., memberOf={dn}),
/// it must be filter-encoded per RFC 4515.
/// </summary>
public static class LdapDnEncoder
{
    /// <summary>
    /// Encodes a DN for safe use inside an LDAP filter assertion.
    /// </summary>
    public static string EncodeForFilter(string dn)
    {
        return LdapFilterEncoder.Encode(dn);
    }
}
