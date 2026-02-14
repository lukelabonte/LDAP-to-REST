namespace LdapToRest.Services;

using System.DirectoryServices.Protocols;
using LdapToRest.Configuration;
using LdapToRest.Models;

public class LdapGroupService : ILdapGroupService
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly LdapSettings _settings;

    private static readonly string[] GroupAttributes =
    {
        "description", "distinguishedname", "gidnumber", "info",
        "managedby", "mail", "memberof", "member", "samaccountname"
    };

    private static readonly string[] MemberAttributes =
    {
        "samaccountname", "displayname", "objectclass", "distinguishedname"
    };

    private static readonly HashSet<string> AllowedWriteAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "description", "displayname"
    };

    public LdapGroupService(ILdapConnectionFactory connectionFactory, LdapSettings settings)
    {
        _connectionFactory = connectionFactory;
        _settings = settings;
    }

    public GroupDto? FindBySamAccountName(string samAccountName, string callerUsername, string callerPassword)
    {
        var filter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(samAccountName)}))";
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);
        var results = connection.Search(_settings.BaseDn, filter, GroupAttributes, SearchScope.Subtree);
        return results.Count == 0 ? null : MapToGroupDto(results[0]);
    }

    public GroupDto? FindByDistinguishedName(string distinguishedName, string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);
        var results = connection.Search(distinguishedName, "(objectClass=group)", GroupAttributes, SearchScope.Base);
        return results.Count == 0 ? null : MapToGroupDto(results[0]);
    }

    public void UpdateGroup(string samAccountName, Dictionary<string, object?> modifications, string callerUsername, string callerPassword)
    {
        foreach (var key in modifications.Keys)
        {
            if (!AllowedWriteAttributes.Contains(key))
                throw new ArgumentException($"Attribute '{key}' is not modifiable.");
        }

        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        var filter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(samAccountName)}))";
        var results = connection.Search(_settings.BaseDn, filter, ["distinguishedname"], SearchScope.Subtree);
        if (results.Count == 0)
            throw new ArgumentException($"Group '{samAccountName}' not found.");

        var groupDn = results[0].DistinguishedName;
        var ldapMods = new List<DirectoryAttributeModification>();

        foreach (var (key, value) in modifications)
        {
            var mod = new DirectoryAttributeModification { Name = key };

            if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            {
                mod.Operation = DirectoryAttributeOperation.Delete;
            }
            else
            {
                mod.Operation = DirectoryAttributeOperation.Replace;
                mod.Add(value.ToString()!);
            }

            ldapMods.Add(mod);
        }

        if (ldapMods.Count > 0)
            connection.Modify(groupDn, ldapMods.ToArray());
    }

    public PaginatedResult<GroupMemberDto> GetMembers(
        string samAccountName, bool recursive, int page, int pageSize,
        string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        // Find the group first
        var groupFilter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(samAccountName)}))";
        var groupResults = connection.Search(_settings.BaseDn, groupFilter, GroupAttributes, SearchScope.Subtree);
        if (groupResults.Count == 0)
            throw new ArgumentException($"Group '{samAccountName}' not found.");

        var groupEntry = groupResults[0];
        List<GroupMemberDto> allMembers;

        if (recursive)
        {
            // Use AD's matching rule in chain OID for recursive membership
            var groupDnEncoded = LdapDnEncoder.EncodeForFilter(groupEntry.DistinguishedName);
            var memberFilter = $"(memberOf:1.2.840.113556.1.4.1941:={groupDnEncoded})";
            var memberResults = connection.Search(_settings.BaseDn, memberFilter, MemberAttributes, SearchScope.Subtree);
            allMembers = memberResults.Select(MapToMemberDto).ToList();
        }
        else
        {
            // Non-recursive: read member attribute and resolve each DN
            var memberDns = groupEntry.GetMultiValueAttribute("member") ?? [];
            allMembers = new List<GroupMemberDto>();
            foreach (var memberDn in memberDns)
            {
                var memberResults = connection.Search(memberDn, "(objectClass=*)", MemberAttributes, SearchScope.Base);
                if (memberResults.Count > 0)
                    allMembers.Add(MapToMemberDto(memberResults[0]));
            }
        }

        var totalCount = allMembers.Count;
        var pagedItems = allMembers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<GroupMemberDto>
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public MembershipCheckResult CheckMembership(
        string groupSamAccountName, string memberSamAccountName,
        string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        // Find the group
        var groupFilter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(groupSamAccountName)}))";
        var groupResults = connection.Search(_settings.BaseDn, groupFilter, ["distinguishedname"], SearchScope.Subtree);
        if (groupResults.Count == 0)
            throw new ArgumentException($"Group '{groupSamAccountName}' not found.");

        var groupDn = groupResults[0].DistinguishedName;

        // Find the member
        var memberFilter = $"(&(objectClass=user)(sAMAccountName={LdapFilterEncoder.Encode(memberSamAccountName)}))";
        var memberResults = connection.Search(_settings.BaseDn, memberFilter, ["distinguishedname"], SearchScope.Subtree);
        if (memberResults.Count == 0)
            throw new ArgumentException($"User '{memberSamAccountName}' not found.");

        var memberDn = memberResults[0].DistinguishedName;

        // Check membership using matching rule in chain (recursive check)
        var groupDnEncoded = LdapDnEncoder.EncodeForFilter(groupDn);
        var checkFilter = $"(memberOf:1.2.840.113556.1.4.1941:={groupDnEncoded})";
        var checkResults = connection.Search(memberDn, checkFilter, ["distinguishedname"], SearchScope.Base);

        return new MembershipCheckResult
        {
            IsMember = checkResults.Count > 0,
            MemberDistinguishedName = memberDn,
            GroupDistinguishedName = groupDn
        };
    }

    public void AddMember(string groupSamAccountName, string memberDn, string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        // Find the group
        var groupFilter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(groupSamAccountName)}))";
        var groupResults = connection.Search(_settings.BaseDn, groupFilter, ["distinguishedname"], SearchScope.Subtree);
        if (groupResults.Count == 0)
            throw new ArgumentException($"Group '{groupSamAccountName}' not found.");

        var groupDn = groupResults[0].DistinguishedName;

        var mod = new DirectoryAttributeModification
        {
            Name = "member",
            Operation = DirectoryAttributeOperation.Add
        };
        mod.Add(memberDn);

        connection.Modify(groupDn, mod);
    }

    public void RemoveMember(string groupSamAccountName, string memberSamAccountName, string callerUsername, string callerPassword)
    {
        using var connection = _connectionFactory.CreateConnection(callerUsername, callerPassword);

        // Find the group
        var groupFilter = $"(&(objectClass=group)(sAMAccountName={LdapFilterEncoder.Encode(groupSamAccountName)}))";
        var groupResults = connection.Search(_settings.BaseDn, groupFilter, ["distinguishedname"], SearchScope.Subtree);
        if (groupResults.Count == 0)
            throw new ArgumentException($"Group '{groupSamAccountName}' not found.");

        var groupDn = groupResults[0].DistinguishedName;

        // Find the member's DN
        var memberFilter = $"(&(objectClass=user)(sAMAccountName={LdapFilterEncoder.Encode(memberSamAccountName)}))";
        var memberResults = connection.Search(_settings.BaseDn, memberFilter, ["distinguishedname"], SearchScope.Subtree);
        if (memberResults.Count == 0)
            throw new ArgumentException($"User '{memberSamAccountName}' not found.");

        var memberDn = memberResults[0].DistinguishedName;

        var mod = new DirectoryAttributeModification
        {
            Name = "member",
            Operation = DirectoryAttributeOperation.Delete
        };
        mod.Add(memberDn);

        connection.Modify(groupDn, mod);
    }

    private static GroupDto MapToGroupDto(LdapEntry entry)
    {
        return new GroupDto
        {
            Description = entry.GetAttribute("description"),
            DistinguishedName = entry.DistinguishedName,
            GidNumber = entry.GetAttribute("gidnumber"),
            Info = entry.GetAttribute("info"),
            ManagedBy = entry.GetAttribute("managedby"),
            Mail = entry.GetAttribute("mail"),
            MemberOf = entry.GetMultiValueAttribute("memberof"),
            Member = entry.GetMultiValueAttribute("member"),
            SamAccountName = entry.GetAttribute("samaccountname")
        };
    }

    private static GroupMemberDto MapToMemberDto(LdapEntry entry)
    {
        return new GroupMemberDto
        {
            DistinguishedName = entry.DistinguishedName,
            SamAccountName = entry.GetAttribute("samaccountname"),
            DisplayName = entry.GetAttribute("displayname"),
            ObjectClass = entry.GetAttribute("objectclass")
        };
    }
}
