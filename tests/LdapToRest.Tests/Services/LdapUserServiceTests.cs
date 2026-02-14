namespace LdapToRest.Tests.Services;

using System.DirectoryServices.Protocols;
using LdapToRest.Configuration;
using LdapToRest.Services;
using Moq;

public class LdapUserServiceTests
{
    private readonly Mock<ILdapConnectionFactory> _mockFactory;
    private readonly Mock<ILdapConnection> _mockConnection;
    private readonly LdapSettings _settings;
    private readonly LdapUserService _service;

    public LdapUserServiceTests()
    {
        _mockFactory = new Mock<ILdapConnectionFactory>();
        _mockConnection = new Mock<ILdapConnection>();
        _mockFactory.Setup(f => f.CreateConnection(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(_mockConnection.Object);
        _settings = new LdapSettings { BaseDn = "DC=example,DC=com" };
        _service = new LdapUserService(_mockFactory.Object, _settings);
    }

    private static LdapEntry CreateUserEntry(
        string dn = "CN=John Smith,OU=Users,DC=example,DC=com",
        string? samAccountName = "jsmith",
        string? displayName = "John Smith",
        string? givenName = "John",
        string? sn = "Smith",
        string? mail = "jsmith@example.com",
        string? department = "Engineering",
        string? title = "Developer",
        string? company = "Example Corp",
        string? description = "A user",
        string? manager = "CN=Boss,OU=Users,DC=example,DC=com",
        string? userPrincipalName = "jsmith@example.com",
        string? whenChanged = "20240101120000.0Z",
        string? userAccountControl = "512",
        string[]? memberOf = null)
    {
        var entry = new LdapEntry { DistinguishedName = dn };
        if (samAccountName != null) entry.SetAttribute("samaccountname", samAccountName);
        if (displayName != null) entry.SetAttribute("displayname", displayName);
        if (givenName != null) entry.SetAttribute("givenname", givenName);
        if (sn != null) entry.SetAttribute("sn", sn);
        if (mail != null) entry.SetAttribute("mail", mail);
        if (department != null) entry.SetAttribute("department", department);
        if (title != null) entry.SetAttribute("title", title);
        if (company != null) entry.SetAttribute("company", company);
        if (description != null) entry.SetAttribute("description", description);
        if (manager != null) entry.SetAttribute("manager", manager);
        if (userPrincipalName != null) entry.SetAttribute("userprincipalname", userPrincipalName);
        if (whenChanged != null) entry.SetAttribute("whenchanged", whenChanged);
        if (userAccountControl != null) entry.SetAttribute("useraccountcontrol", userAccountControl);
        memberOf ??= ["CN=Developers,OU=Groups,DC=example,DC=com"];
        entry.SetAttribute("memberof", memberOf);
        return entry;
    }

    [Fact]
    public void FindBySamAccountName_UserExists_ReturnsDto()
    {
        var entry = CreateUserEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var result = _service.FindBySamAccountName("jsmith", "admin", "pass");

        Assert.NotNull(result);
        Assert.Equal("jsmith", result.SamAccountName);
        Assert.Equal("John Smith", result.DisplayName);
        Assert.Equal("John", result.GivenName);
        Assert.Equal("Smith", result.Sn);
        Assert.Equal("jsmith@example.com", result.Mail);
        Assert.Equal("Engineering", result.Department);
        Assert.Equal("Developer", result.Title);
        Assert.Equal("Example Corp", result.Company);
        Assert.Equal("A user", result.Description);
        Assert.Equal("CN=Boss,OU=Users,DC=example,DC=com", result.Manager);
        Assert.Equal("jsmith@example.com", result.UserPrincipalName);
        Assert.Equal("CN=John Smith,OU=Users,DC=example,DC=com", result.DistinguishedName);
        Assert.NotNull(result.MemberOf);
        Assert.Contains("CN=Developers,OU=Groups,DC=example,DC=com", result.MemberOf);
    }

    [Fact]
    public void FindBySamAccountName_UserNotFound_ReturnsNull()
    {
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([]);

        var result = _service.FindBySamAccountName("nonexistent", "admin", "pass");

        Assert.Null(result);
    }

    [Fact]
    public void FindBySamAccountName_EnabledUser_EnabledIsTrue()
    {
        // userAccountControl=512 means normal account, NOT disabled
        var entry = CreateUserEntry(userAccountControl: "512");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var result = _service.FindBySamAccountName("jsmith", "admin", "pass");

        Assert.True(result!.Enabled);
    }

    [Fact]
    public void FindBySamAccountName_DisabledUser_EnabledIsFalse()
    {
        // userAccountControl=514 means ACCOUNTDISABLE (0x0002) bit is set
        var entry = CreateUserEntry(userAccountControl: "514");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var result = _service.FindBySamAccountName("jsmith", "admin", "pass");

        Assert.False(result!.Enabled);
    }

    [Fact]
    public void FindBySamAccountName_MissingAttributes_HandlesGracefully()
    {
        // Entry with only DN and samAccountName, everything else missing
        var entry = new LdapEntry { DistinguishedName = "CN=Sparse,DC=example,DC=com" };
        entry.SetAttribute("samaccountname", "sparse");
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var result = _service.FindBySamAccountName("sparse", "admin", "pass");

        Assert.NotNull(result);
        Assert.Equal("sparse", result.SamAccountName);
        Assert.Null(result.DisplayName);
        Assert.Null(result.Mail);
        Assert.Null(result.Department);
        Assert.Null(result.Enabled);
        Assert.Null(result.MemberOf);
    }

    [Fact]
    public void FindBySamAccountName_UsesEncodedFilter()
    {
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([]);

        _service.FindBySamAccountName("jsmith", "admin", "pass");

        _mockConnection.Verify(c => c.Search(
            "DC=example,DC=com",
            "(&(objectClass=user)(sAMAccountName=jsmith))",
            It.IsAny<string[]>(),
            SearchScope.Subtree));
    }

    [Fact]
    public void FindByDistinguishedName_UserExists_ReturnsDto()
    {
        var entry = CreateUserEntry();
        _mockConnection.Setup(c => c.Search(
            "CN=John Smith,OU=Users,DC=example,DC=com",
            "(objectClass=user)",
            It.IsAny<string[]>(),
            SearchScope.Base))
            .Returns([entry]);

        var result = _service.FindByDistinguishedName(
            "CN=John Smith,OU=Users,DC=example,DC=com", "admin", "pass");

        Assert.NotNull(result);
        Assert.Equal("jsmith", result.SamAccountName);
    }

    [Fact]
    public void UpdateUser_InvalidAttribute_ThrowsArgumentException()
    {
        var modifications = new Dictionary<string, object?> { { "invalidattr", "value" } };

        Assert.Throws<ArgumentException>(() =>
            _service.UpdateUser("jsmith", modifications, "admin", "pass"));
    }

    [Fact]
    public void UpdateUser_ValidAttribute_SendsModifyRequest()
    {
        // First, the service needs to find the user to get their DN
        var entry = CreateUserEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var modifications = new Dictionary<string, object?> { { "department", "Sales" } };
        _service.UpdateUser("jsmith", modifications, "admin", "pass");

        _mockConnection.Verify(c => c.Modify(
            "CN=John Smith,OU=Users,DC=example,DC=com",
            It.Is<DirectoryAttributeModification[]>(mods =>
                mods.Length == 1 &&
                mods[0].Name == "department" &&
                mods[0].Operation == DirectoryAttributeOperation.Replace)));
    }

    [Fact]
    public void UpdateUser_NullValue_SendsDeleteOperation()
    {
        var entry = CreateUserEntry();
        _mockConnection.Setup(c => c.Search(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), SearchScope.Subtree))
            .Returns([entry]);

        var modifications = new Dictionary<string, object?> { { "description", null } };
        _service.UpdateUser("jsmith", modifications, "admin", "pass");

        _mockConnection.Verify(c => c.Modify(
            It.IsAny<string>(),
            It.Is<DirectoryAttributeModification[]>(mods =>
                mods.Length == 1 &&
                mods[0].Name == "description" &&
                mods[0].Operation == DirectoryAttributeOperation.Delete)));
    }
}
