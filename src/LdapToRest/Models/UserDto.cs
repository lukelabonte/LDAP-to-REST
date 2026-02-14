namespace LdapToRest.Models;

/// <summary>
/// An Active Directory user account with its standard attributes.
/// </summary>
public class UserDto
{
    /// <summary>Company name (AD attribute: company)</summary>
    public string? Company { get; set; }

    /// <summary>Department name (AD attribute: department)</summary>
    public string? Department { get; set; }

    /// <summary>Free-text description of the user (AD attribute: description)</summary>
    public string? Description { get; set; }

    /// <summary>Full LDAP path to the user (e.g., CN=John Smith,OU=Users,DC=example,DC=com)</summary>
    public string? DistinguishedName { get; set; }

    /// <summary>Display name shown in the address book (AD attribute: displayName)</summary>
    public string? DisplayName { get; set; }

    /// <summary>First name (AD attribute: givenName)</summary>
    public string? GivenName { get; set; }

    /// <summary>Whether the account is enabled. Derived from the ACCOUNTDISABLE bit (0x0002) in userAccountControl.</summary>
    public bool? Enabled { get; set; }

    /// <summary>Last name / surname (AD attribute: sn)</summary>
    public string? Sn { get; set; }

    /// <summary>Email address (AD attribute: mail)</summary>
    public string? Mail { get; set; }

    /// <summary>DN of the user's manager (AD attribute: manager)</summary>
    public string? Manager { get; set; }

    /// <summary>List of group DNs this user belongs to (AD attribute: memberOf)</summary>
    public string[]? MemberOf { get; set; }

    /// <summary>AD login name — the short username used to sign in (AD attribute: sAMAccountName)</summary>
    public string? SamAccountName { get; set; }

    /// <summary>Job title (AD attribute: title)</summary>
    public string? Title { get; set; }

    /// <summary>User Principal Name, typically in email format like user@domain.com (AD attribute: userPrincipalName)</summary>
    public string? UserPrincipalName { get; set; }

    /// <summary>When this user was last modified in AD (AD attribute: whenChanged)</summary>
    public string? WhenChanged { get; set; }
}
