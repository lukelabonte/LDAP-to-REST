namespace LdapToRest.Services;

using LdapToRest.Models;

public interface ILdapUserService
{
    UserDto? FindBySamAccountName(string samAccountName, string callerUsername, string callerPassword);
    UserDto? FindByDistinguishedName(string distinguishedName, string callerUsername, string callerPassword);
    void UpdateUser(string samAccountName, Dictionary<string, object?> modifications, string callerUsername, string callerPassword);
}
