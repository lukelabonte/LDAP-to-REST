namespace LdapToRest.Models;

/// <summary>
/// Request body for adding a member to a group.
/// </summary>
public class AddGroupMemberRequest
{
    /// <summary>
    /// The full Distinguished Name (DN) of the user or group to add.
    /// Get this from the `distinguishedName` field returned by `GET /api/users/{samAccountName}`.
    /// Example: `CN=John Smith,OU=Users,DC=example,DC=com`
    /// </summary>
    public required string DistinguishedName { get; set; }
}
