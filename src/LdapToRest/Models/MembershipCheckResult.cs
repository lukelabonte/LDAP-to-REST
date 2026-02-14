namespace LdapToRest.Models;

public class MembershipCheckResult
{
    public bool IsMember { get; set; }
    public string? MemberDistinguishedName { get; set; }
    public string? GroupDistinguishedName { get; set; }
}
