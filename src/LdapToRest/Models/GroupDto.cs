namespace LdapToRest.Models;

/// <summary>
/// An Active Directory group with its standard attributes.
/// </summary>
public class GroupDto
{
    /// <summary>Free-text description of the group's purpose (AD attribute: description)</summary>
    public string? Description { get; set; }

    /// <summary>Full LDAP path to the group (e.g., CN=Developers,OU=Groups,DC=example,DC=com)</summary>
    public string? DistinguishedName { get; set; }

    /// <summary>POSIX group ID number, if set (AD attribute: gidNumber)</summary>
    public string? GidNumber { get; set; }

    /// <summary>Additional notes/comments about the group (AD attribute: info)</summary>
    public string? Info { get; set; }

    /// <summary>DN of the user or group that manages this group (AD attribute: managedBy)</summary>
    public string? ManagedBy { get; set; }

    /// <summary>Email address for the group, if any (AD attribute: mail)</summary>
    public string? Mail { get; set; }

    /// <summary>List of group DNs this group belongs to — parent groups (AD attribute: memberOf)</summary>
    public string[]? MemberOf { get; set; }

    /// <summary>List of DNs of direct members (users and nested groups) in this group (AD attribute: member)</summary>
    public string[]? Member { get; set; }

    /// <summary>AD login name — the short group name (AD attribute: sAMAccountName)</summary>
    public string? SamAccountName { get; set; }
}
