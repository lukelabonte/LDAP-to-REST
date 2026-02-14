namespace LdapToRest.Models;

/// <summary>
/// Result of checking whether a user is a member of a group.
/// </summary>
public class MembershipCheckResult
{
    /// <summary>Whether the user is a member of the group (includes transitive/nested membership)</summary>
    public bool IsMember { get; set; }

    /// <summary>The resolved DN of the member that was checked</summary>
    public string? MemberDistinguishedName { get; set; }

    /// <summary>The resolved DN of the group that was checked</summary>
    public string? GroupDistinguishedName { get; set; }
}
