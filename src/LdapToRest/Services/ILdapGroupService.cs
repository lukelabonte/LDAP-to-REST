namespace LdapToRest.Services;

using LdapToRest.Models;

public interface ILdapGroupService
{
    GroupDto? FindBySamAccountName(string samAccountName, string callerUsername, string callerPassword);
    GroupDto? FindByDistinguishedName(string distinguishedName, string callerUsername, string callerPassword);
    void UpdateGroup(string samAccountName, Dictionary<string, object?> modifications, string callerUsername, string callerPassword);
    PaginatedResult<GroupMemberDto> GetMembers(string samAccountName, bool recursive, int page, int pageSize, string callerUsername, string callerPassword);
    MembershipCheckResult CheckMembership(string groupSamAccountName, string memberSamAccountName, string callerUsername, string callerPassword);
    void AddMember(string groupSamAccountName, string memberDn, string callerUsername, string callerPassword);
    void RemoveMember(string groupSamAccountName, string memberSamAccountName, string callerUsername, string callerPassword);
}
