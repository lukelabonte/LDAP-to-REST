namespace LdapToRest.Services;

using System.DirectoryServices.Protocols;
using LdapToRest.Configuration;
using LdapToRest.Models;

public class LdapUserService : ILdapUserService
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly LdapSettings _settings;

    private static readonly string[] UserAttributes =
    {
        "company", "department", "description", "distinguishedname",
        "displayname", "givenname", "sn", "mail", "manager", "memberof",
        "samaccountname", "title", "userprincipalname", "whenchanged",
        "useraccountcontrol"
    };

    private static readonly HashSet<string> AllowedWriteAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "company", "department", "description", "displayname", "givenname", "enabled", "sn"
    };

    public LdapUserService(ILdapConnectionFactory connectionFactory, LdapSettings settings)
    {
        _connectionFactory = connectionFactory;
        _settings = settings;
    }

    public UserDto? FindBySamAccountName(string samAccountName, string callerUsername, string callerPassword)
    {
        var filter = $"(&(objectClass=user)(sAMAccountName={LdapFilterEncoder.Encode(samAccountName)}))";
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);
        var results = connection.Search(_settings.BaseDn, filter, UserAttributes, SearchScope.Subtree);
        return results.Count == 0 ? null : MapToUserDto(results[0]);
    }

    public UserDto? FindByDistinguishedName(string distinguishedName, string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);
        var results = connection.Search(distinguishedName, "(objectClass=user)", UserAttributes, SearchScope.Base);
        return results.Count == 0 ? null : MapToUserDto(results[0]);
    }

    public void UpdateUser(string samAccountName, Dictionary<string, object?> modifications, string callerUsername, string callerPassword)
    {
        foreach (var key in modifications.Keys)
        {
            if (!AllowedWriteAttributes.Contains(key))
                throw new ArgumentException($"Attribute '{key}' is not modifiable.");
        }

        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        // Look up the user's DN
        var filter = $"(&(objectClass=user)(sAMAccountName={LdapFilterEncoder.Encode(samAccountName)}))";
        var results = connection.Search(_settings.BaseDn, filter, ["distinguishedname", "useraccountcontrol"], SearchScope.Subtree);
        if (results.Count == 0)
            throw new ArgumentException($"User '{samAccountName}' not found.");

        var userDn = results[0].DistinguishedName;
        var ldapMods = new List<DirectoryAttributeModification>();

        foreach (var (key, value) in modifications)
        {
            if (string.Equals(key, "enabled", StringComparison.OrdinalIgnoreCase))
            {
                // Handle enabled toggle via userAccountControl bit manipulation
                var currentUac = results[0].GetAttribute("useraccountcontrol");
                if (currentUac != null && int.TryParse(currentUac, out var uacValue) && value is bool enabled)
                {
                    var newUac = enabled
                        ? uacValue & ~0x0002   // Clear ACCOUNTDISABLE bit
                        : uacValue | 0x0002;   // Set ACCOUNTDISABLE bit

                    var mod = new DirectoryAttributeModification
                    {
                        Name = "userAccountControl",
                        Operation = DirectoryAttributeOperation.Replace
                    };
                    mod.Add(newUac.ToString());
                    ldapMods.Add(mod);
                }
                continue;
            }

            var attrMod = new DirectoryAttributeModification { Name = key };

            if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            {
                attrMod.Operation = DirectoryAttributeOperation.Delete;
            }
            else
            {
                attrMod.Operation = DirectoryAttributeOperation.Replace;
                attrMod.Add(value.ToString()!);
            }

            ldapMods.Add(attrMod);
        }

        if (ldapMods.Count > 0)
            connection.Modify(userDn, ldapMods.ToArray());
    }

    private static UserDto MapToUserDto(LdapEntry entry)
    {
        return new UserDto
        {
            Company = entry.GetAttribute("company"),
            Department = entry.GetAttribute("department"),
            Description = entry.GetAttribute("description"),
            DistinguishedName = entry.DistinguishedName,
            DisplayName = entry.GetAttribute("displayname"),
            GivenName = entry.GetAttribute("givenname"),
            Sn = entry.GetAttribute("sn"),
            Mail = entry.GetAttribute("mail"),
            Manager = entry.GetAttribute("manager"),
            MemberOf = entry.GetMultiValueAttribute("memberof"),
            SamAccountName = entry.GetAttribute("samaccountname"),
            Title = entry.GetAttribute("title"),
            UserPrincipalName = entry.GetAttribute("userprincipalname"),
            WhenChanged = entry.GetAttribute("whenchanged"),
            Enabled = DeriveEnabled(entry.GetAttribute("useraccountcontrol"))
        };
    }

    private static bool? DeriveEnabled(string? userAccountControl)
    {
        if (userAccountControl == null || !int.TryParse(userAccountControl, out var value))
            return null;
        return (value & 0x0002) == 0;
    }
}
