namespace LdapToRest.Models;

public class GroupDto
{
    public string? Description { get; set; }
    public string? DistinguishedName { get; set; }
    public string? GidNumber { get; set; }
    public string? Info { get; set; }
    public string? ManagedBy { get; set; }
    public string? Mail { get; set; }
    public string[]? MemberOf { get; set; }
    public string[]? Member { get; set; }
    public string? SamAccountName { get; set; }
}
