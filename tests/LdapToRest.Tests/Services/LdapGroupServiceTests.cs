namespace LdapToRest.Tests.Services;

using System.DirectoryServices.Protocols;
using LdapToRest.Configuration;
using LdapToRest.Services;
using Moq;

public class LdapGroupServiceTests
{
    private readonly Mock<ILdapConnectionFactory> _mockFactory;
    private readonly Mock<ILdapConnection> _mockConnection;
    private readonly LdapSettings _settings;
    private readonly LdapGroupService _service;

    public LdapGroupServiceTests()
    {
        _mockFactory = new Mock<ILdapConnectionFactory>();
        _mockConnection = new Mock<ILdapConnection>();
        _mockFactory.Setup(f => f.CreateConnection(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(_mockConnection.Object);
        _settings = new LdapSettings { BaseDn = "DC=example,DC=com" };
        _service = new LdapGroupService(_mockFactory.Object, _settings);
    }

    private static LdapEntry CreateGroupEntry(
        string dn = "CN=Developers,OU=Groups,DC=example,DC=com",
        string? samAccountName = "developers",
        string? description = "Dev team",
        string? gidNumber = "10001",
        string? info = "Some info",
        string? managedBy = "CN=Boss,OU=Users,DC=example,DC=com",
        string? mail = "devs@example.com",
        string[]? member = null,
        string[]? memberOf = null)
    {
        var entry = new LdapEntry { DistinguishedName = dn };
        if (samAccountName != null) entry.SetAttribute("samaccountname", samAccountName);
        if (description != null) entry.SetAttribute("description", description);
        if (gidNumber != null) entry.SetAttribute("gidnumber", gidNumber);
        if (info != null) entry.SetAttribute("info", info);
        if (managedBy != null) entry.SetAttribute("managedby", managedBy);
        if (mail != null) entry.SetAttribute("mail", mail);
        member ??= ["CN=John Smith,OU=Users,DC=example,DC=com", "CN=Jane Doe,OU=Users,DC=example,DC=com"];
        entry.SetAttribute("member", member);
        if (memberOf != null) entry.SetAttribute("memberof", memberOf);
        return entry;
    }

    private static LdapEntry CreateMemberEntry(string dn, string sam, string displayName, string objectClass)
    {
        var entry = new LdapEntry { DistinguishedName = dn };
        entry.SetAttribute("samaccountname", sam);
        entry.SetAttribute("displayname", displayName);
        entry.SetAttribute("objectclass", objectClass);
        return entry;
    }

    [Fact]
    public void FindBySamAccountName_GroupExists_ReturnsDto()
    {
        var entry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var result = _service.FindBySamAccountName("developers", "admin", "pass");

        Assert.NotNull(result);
        Assert.Equal("developers", result.SamAccountName);
        Assert.Equal("Dev team", result.Description);
        Assert.Equal("10001", result.GidNumber);
        Assert.Equal("Some info", result.Info);
        Assert.Equal("CN=Boss,OU=Users,DC=example,DC=com", result.ManagedBy);
        Assert.Equal("devs@example.com", result.Mail);
        Assert.NotNull(result.Member);
        Assert.Equal(2, result.Member!.Length);
    }

    [Fact]
    public void GetMembers_NonRecursive_ReturnsMemberDtos()
    {
        // First search: find the group to get its member attribute
        var groupEntry = CreateGroupEntry(member: [
            "CN=John Smith,OU=Users,DC=example,DC=com",
            "CN=Jane Doe,OU=Users,DC=example,DC=com"
        ]);
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        // Subsequent searches: resolve each member DN
        _mockConnection.Setup(c => c.Search(
            "CN=John Smith,OU=Users,DC=example,DC=com", It.IsAny<string>(),
            It.IsAny<string[]>(), SearchScope.Base))
            .Returns([CreateMemberEntry("CN=John Smith,OU=Users,DC=example,DC=com", "jsmith", "John Smith", "user")]);

        _mockConnection.Setup(c => c.Search(
            "CN=Jane Doe,OU=Users,DC=example,DC=com", It.IsAny<string>(),
            It.IsAny<string[]>(), SearchScope.Base))
            .Returns([CreateMemberEntry("CN=Jane Doe,OU=Users,DC=example,DC=com", "jdoe", "Jane Doe", "user")]);

        var result = _service.GetMembers("developers", recursive: false, page: 1, pageSize: 50, "admin", "pass");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, m => m.SamAccountName == "jsmith");
        Assert.Contains(result.Items, m => m.SamAccountName == "jdoe");
    }

    [Fact]
    public void GetMembers_Recursive_UsesMatchingRuleInChain()
    {
        var groupEntry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        // Recursive search should use OID 1.2.840.113556.1.4.1941
        _mockConnection.Setup(c => c.Search(
            "DC=example,DC=com",
            It.Is<string>(f => f.Contains("1.2.840.113556.1.4.1941")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([CreateMemberEntry("CN=John Smith,OU=Users,DC=example,DC=com", "jsmith", "John Smith", "user")]);

        var result = _service.GetMembers("developers", recursive: true, page: 1, pageSize: 50, "admin", "pass");

        _mockConnection.Verify(c => c.Search(
            "DC=example,DC=com",
            It.Is<string>(f => f.Contains("1.2.840.113556.1.4.1941")),
            It.IsAny<string[]>(), SearchScope.Subtree));
    }

    [Fact]
    public void GetMembers_EmptyGroup_ReturnsEmptyList()
    {
        var groupEntry = CreateGroupEntry(member: []);
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        var result = _service.GetMembers("developers", recursive: false, page: 1, pageSize: 50, "admin", "pass");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetMembers_Pagination_ReturnsCorrectPage()
    {
        var members = Enumerable.Range(1, 5).Select(i =>
            $"CN=User{i},OU=Users,DC=example,DC=com").ToArray();
        var groupEntry = CreateGroupEntry(member: members);
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        foreach (var m in members)
        {
            var num = m.Split("User")[1].Split(",")[0];
            _mockConnection.Setup(c => c.Search(m, It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Base))
                .Returns([CreateMemberEntry(m, $"user{num}", $"User {num}", "user")]);
        }

        var result = _service.GetMembers("developers", recursive: false, page: 2, pageSize: 2, "admin", "pass");

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void CheckMembership_IsMember_ReturnsTrue()
    {
        // Find the group
        var groupEntry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group") && f.Contains("developers")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        // Find the member
        var memberEntry = CreateMemberEntry("CN=John Smith,OU=Users,DC=example,DC=com", "jsmith", "John Smith", "user");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=user") && f.Contains("jsmith")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([memberEntry]);

        // Check membership — use matching rule in chain
        _mockConnection.Setup(c => c.Search(
            "CN=John Smith,OU=Users,DC=example,DC=com",
            It.Is<string>(f => f.Contains("1.2.840.113556.1.4.1941")),
            It.IsAny<string[]>(), SearchScope.Base))
            .Returns([memberEntry]);

        var result = _service.CheckMembership("developers", "jsmith", "admin", "pass");

        Assert.True(result.IsMember);
        Assert.Equal("CN=John Smith,OU=Users,DC=example,DC=com", result.MemberDistinguishedName);
        Assert.Equal("CN=Developers,OU=Groups,DC=example,DC=com", result.GroupDistinguishedName);
    }

    [Fact]
    public void CheckMembership_IsNotMember_ReturnsFalse()
    {
        var groupEntry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group") && f.Contains("developers")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        var memberEntry = CreateMemberEntry("CN=Outside User,OU=Users,DC=example,DC=com", "outsider", "Outside User", "user");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=user") && f.Contains("outsider")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([memberEntry]);

        // Membership check returns empty — not a member
        _mockConnection.Setup(c => c.Search(
            "CN=Outside User,OU=Users,DC=example,DC=com",
            It.Is<string>(f => f.Contains("1.2.840.113556.1.4.1941")),
            It.IsAny<string[]>(), SearchScope.Base))
            .Returns([]);

        var result = _service.CheckMembership("developers", "outsider", "admin", "pass");

        Assert.False(result.IsMember);
    }

    [Fact]
    public void AddMember_ValidDn_SendsAddModification()
    {
        var groupEntry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        _service.AddMember("developers", "CN=New User,OU=Users,DC=example,DC=com", "admin", "pass");

        _mockConnection.Verify(c => c.Modify(
            "CN=Developers,OU=Groups,DC=example,DC=com",
            It.Is<DirectoryAttributeModification[]>(mods =>
                mods.Length == 1 &&
                mods[0].Name == "member" &&
                mods[0].Operation == DirectoryAttributeOperation.Add)));
    }

    [Fact]
    public void RemoveMember_ValidName_SendsDeleteModification()
    {
        // Find the group
        var groupEntry = CreateGroupEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=group") && f.Contains("developers")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([groupEntry]);

        // Find the member to get their DN
        var memberEntry = CreateMemberEntry("CN=John Smith,OU=Users,DC=example,DC=com", "jsmith", "John Smith", "user");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.Is<string>(f => f.Contains("objectClass=user") && f.Contains("jsmith")),
            It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([memberEntry]);

        _service.RemoveMember("developers", "jsmith", "admin", "pass");

        _mockConnection.Verify(c => c.Modify(
            "CN=Developers,OU=Groups,DC=example,DC=com",
            It.Is<DirectoryAttributeModification[]>(mods =>
                mods.Length == 1 &&
                mods[0].Name == "member" &&
                mods[0].Operation == DirectoryAttributeOperation.Delete)));
    }
}
