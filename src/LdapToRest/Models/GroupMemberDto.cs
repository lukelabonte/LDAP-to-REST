namespace LdapToRest.Models;

/// <summary>
/// A member of an Active Directory group (returned in paginated member lists).
/// </summary>
public class GroupMemberDto
{
    /// <summary>Full LDAP path to the member (e.g., CN=John Smith,OU=Users,DC=example,DC=com)</summary>
    public string? DistinguishedName { get; set; }

    /// <summary>The member's AD login name (e.g., jsmith)</summary>
    public string? SamAccountName { get; set; }

    /// <summary>The member's display name (e.g., John Smith)</summary>
    public string? DisplayName { get; set; }

    /// <summary>The type of AD object — typically "user" or "group" (for nested groups)</summary>
    public string? ObjectClass { get; set; }
}
