namespace LdapToRest.Tests.Controllers;

using System.Text.Json;
using LdapToRest.Controllers;
using LdapToRest.Middleware;
using LdapToRest.Models;
using LdapToRest.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class UsersControllerTests
{
    private readonly Mock<ILdapUserService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<ILdapUserService>();
        _controller = new UsersController(_mockUserService.Object);

        // Set up HttpContext with credentials
        var httpContext = new DefaultHttpContext();
        httpContext.Items[BasicAuthenticationHandler.UsernameKey] = "admin";
        httpContext.Items[BasicAuthenticationHandler.PasswordKey] = "pass";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void Get_ExistingUser_Returns200WithDto()
    {
        var dto = new UserDto { SamAccountName = "jsmith", DisplayName = "John Smith" };
        _mockUserService.Setup(s => s.FindBySamAccountName("jsmith", "admin", "pass"))
                        .Returns(dto);

        var result = _controller.GetBySamAccountName("jsmith");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal("jsmith", returnedDto.SamAccountName);
    }

    [Fact]
    public void Get_NonexistentUser_Returns404()
    {
        _mockUserService.Setup(s => s.FindBySamAccountName("nobody", "admin", "pass"))
                        .Returns((UserDto?)null);

        var result = _controller.GetBySamAccountName("nobody");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Get_InvalidSamAccountName_ThrowsArgumentException()
    {
        // Characters like * are disallowed
        Assert.Throws<ArgumentException>(() => _controller.GetBySamAccountName("user*name"));
    }

    [Fact]
    public void GetByDn_ExistingUser_Returns200()
    {
        var dn = "CN=John Smith,OU=Users,DC=example,DC=com";
        var dto = new UserDto { SamAccountName = "jsmith" };
        _mockUserService.Setup(s => s.FindByDistinguishedName(dn, "admin", "pass"))
                        .Returns(dto);

        var result = _controller.GetByDistinguishedName(dn);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void Patch_ValidBody_Returns204()
    {
        var body = JsonSerializer.Deserialize<JsonElement>("""{"department": "Sales"}""");

        var result = _controller.Update("jsmith", body);

        Assert.IsType<NoContentResult>(result);
        _mockUserService.Verify(s => s.UpdateUser(
            "jsmith",
            It.Is<Dictionary<string, object?>>(d => d.ContainsKey("department")),
            "admin", "pass"));
    }

    [Fact]
    public void Patch_InvalidAttribute_ThrowsArgumentException()
    {
        var body = JsonSerializer.Deserialize<JsonElement>("""{"invalidattr": "value"}""");

        Assert.Throws<ArgumentException>(() => _controller.Update("jsmith", body));
    }
}
