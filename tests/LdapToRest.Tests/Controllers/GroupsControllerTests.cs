namespace LdapToRest.Tests.Controllers;

using LdapToRest.Controllers;
using LdapToRest.Middleware;
using LdapToRest.Models;
using LdapToRest.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class GroupsControllerTests
{
    private readonly Mock<ILdapGroupService> _mockGroupService;
    private readonly GroupsController _controller;

    public GroupsControllerTests()
    {
        _mockGroupService = new Mock<ILdapGroupService>();
        _controller = new GroupsController(_mockGroupService.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[BasicAuthenticationHandler.UsernameKey] = "admin";
        httpContext.Items[BasicAuthenticationHandler.PasswordKey] = "pass";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void Get_ExistingGroup_Returns200()
    {
        var dto = new GroupDto { SamAccountName = "developers" };
        _mockGroupService.Setup(s => s.FindBySamAccountName("developers", "admin", "pass"))
                         .Returns(dto);

        var result = _controller.GetBySamAccountName("developers");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void Get_NonexistentGroup_Returns404()
    {
        _mockGroupService.Setup(s => s.FindBySamAccountName("nobody", "admin", "pass"))
                         .Returns((GroupDto?)null);

        var result = _controller.GetBySamAccountName("nobody");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetMembers_Default_NonRecursive()
    {
        var paginatedResult = new PaginatedResult<GroupMemberDto>
        {
            Items = [new GroupMemberDto { SamAccountName = "jsmith" }],
            Page = 1,
            PageSize = 50,
            TotalCount = 1
        };
        _mockGroupService.Setup(s => s.GetMembers("developers", false, 1, 50, "admin", "pass"))
                         .Returns(paginatedResult);

        var result = _controller.GetMembers("developers");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        // Verify default parameters: recursive=false, page=1, pageSize=50
        _mockGroupService.Verify(s => s.GetMembers("developers", false, 1, 50, "admin", "pass"));
    }

    [Fact]
    public void GetMembers_Recursive_PassesTrueToService()
    {
        var paginatedResult = new PaginatedResult<GroupMemberDto>
        {
            Items = [],
            Page = 1,
            PageSize = 50,
            TotalCount = 0
        };
        _mockGroupService.Setup(s => s.GetMembers("developers", true, 1, 50, "admin", "pass"))
                         .Returns(paginatedResult);

        _controller.GetMembers("developers", recursive: true);

        _mockGroupService.Verify(s => s.GetMembers("developers", true, 1, 50, "admin", "pass"));
    }

    [Fact]
    public void CheckMembership_IsMember_Returns200()
    {
        var checkResult = new MembershipCheckResult
        {
            IsMember = true,
            MemberDistinguishedName = "CN=John,DC=example,DC=com",
            GroupDistinguishedName = "CN=Devs,DC=example,DC=com"
        };
        _mockGroupService.Setup(s => s.CheckMembership("developers", "jsmith", "admin", "pass"))
                         .Returns(checkResult);

        var result = _controller.CheckMembership("developers", "jsmith");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void AddMember_ValidDn_Returns201()
    {
        var request = new AddGroupMemberRequest
        {
            DistinguishedName = "CN=New User,OU=Users,DC=example,DC=com"
        };

        var result = _controller.AddMember("developers", request);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(201, statusResult.StatusCode);
        _mockGroupService.Verify(s => s.AddMember(
            "developers", "CN=New User,OU=Users,DC=example,DC=com", "admin", "pass"));
    }

    [Fact]
    public void RemoveMember_Success_Returns204()
    {
        var result = _controller.RemoveMember("developers", "jsmith");

        Assert.IsType<NoContentResult>(result);
        _mockGroupService.Verify(s => s.RemoveMember("developers", "jsmith", "admin", "pass"));
    }

    [Fact]
    public void AddMember_InvalidDn_ThrowsArgumentException()
    {
        var request = new AddGroupMemberRequest { DistinguishedName = "" };

        Assert.Throws<ArgumentException>(() => _controller.AddMember("developers", request));
    }
}
