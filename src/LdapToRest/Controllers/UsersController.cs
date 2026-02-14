namespace LdapToRest.Controllers;

using System.Text.Json;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : LdapApiController
{
    private readonly ILdapUserService _userService;

    private static readonly HashSet<string> AllowedWriteAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "company", "department", "description", "displayname", "givenname", "enabled", "sn"
    };

    public UsersController(ILdapUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{samAccountName}")]
    public IActionResult GetBySamAccountName(string samAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var (username, password) = ExtractCredentials();
        var user = _userService.FindBySamAccountName(samAccountName, username, password);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet("dn/{*distinguishedName}")]
    public IActionResult GetByDistinguishedName(string distinguishedName)
    {
        InputValidator.ValidateDistinguishedName(distinguishedName);
        var (username, password) = ExtractCredentials();
        var user = _userService.FindByDistinguishedName(distinguishedName, username, password);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPatch("{samAccountName}")]
    public IActionResult Update(string samAccountName, [FromBody] JsonElement body)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var modifications = ParseAndValidateModifications(body, AllowedWriteAttributes);
        var (username, password) = ExtractCredentials();
        _userService.UpdateUser(samAccountName, modifications, username, password);
        return NoContent();
    }
}
