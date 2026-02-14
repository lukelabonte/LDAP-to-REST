namespace LdapToRest.Models;

public class UserDto
{
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
    public string? DistinguishedName { get; set; }
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public bool? Enabled { get; set; }
    public string? Sn { get; set; }
    public string? Mail { get; set; }
    public string? Manager { get; set; }
    public string[]? MemberOf { get; set; }
    public string? SamAccountName { get; set; }
    public string? Title { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? WhenChanged { get; set; }
}
