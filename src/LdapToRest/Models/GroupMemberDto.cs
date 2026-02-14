namespace LdapToRest.Models;

public class GroupMemberDto
{
    public string? DistinguishedName { get; set; }
    public string? SamAccountName { get; set; }
    public string? DisplayName { get; set; }
    public string? ObjectClass { get; set; }
}
