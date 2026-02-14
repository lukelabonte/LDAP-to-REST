namespace LdapToRest.Controllers;

using System.Text.Json;
using LdapToRest.Models;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : LdapApiController
{
    private readonly ILdapGroupService _groupService;

    private static readonly HashSet<string> AllowedWriteAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "description", "displayname"
    };

    public GroupsController(ILdapGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet("{samAccountName}")]
    public IActionResult GetBySamAccountName(string samAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var (username, password) = ExtractCredentials();
        var group = _groupService.FindBySamAccountName(samAccountName, username, password);
        return group == null ? NotFound() : Ok(group);
    }

    [HttpGet("dn/{*distinguishedName}")]
    public IActionResult GetByDistinguishedName(string distinguishedName)
    {
        InputValidator.ValidateDistinguishedName(distinguishedName);
        var (username, password) = ExtractCredentials();
        var group = _groupService.FindByDistinguishedName(distinguishedName, username, password);
        return group == null ? NotFound() : Ok(group);
    }

    [HttpPatch("{samAccountName}")]
    public IActionResult Update(string samAccountName, [FromBody] JsonElement body)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var modifications = ParseAndValidateModifications(body, AllowedWriteAttributes);
        var (username, password) = ExtractCredentials();
        _groupService.UpdateGroup(samAccountName, modifications, username, password);
        return NoContent();
    }

    [HttpGet("{samAccountName}/members")]
    public IActionResult GetMembers(
        string samAccountName,
        [FromQuery] bool recursive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var (username, password) = ExtractCredentials();
        var result = _groupService.GetMembers(samAccountName, recursive, page, pageSize, username, password);
        return Ok(result);
    }

    [HttpGet("{samAccountName}/members/{memberSamAccountName}")]
    public IActionResult CheckMembership(string samAccountName, string memberSamAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateSamAccountName(memberSamAccountName);
        var (username, password) = ExtractCredentials();
        var result = _groupService.CheckMembership(samAccountName, memberSamAccountName, username, password);
        return Ok(result);
    }

    [HttpPost("{samAccountName}/members")]
    public IActionResult AddMember(string samAccountName, [FromBody] AddGroupMemberRequest request)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateDistinguishedName(request.DistinguishedName);
        var (username, password) = ExtractCredentials();
        _groupService.AddMember(samAccountName, request.DistinguishedName, username, password);
        return StatusCode(201);
    }

    [HttpDelete("{samAccountName}/members/{memberSamAccountName}")]
    public IActionResult RemoveMember(string samAccountName, string memberSamAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateSamAccountName(memberSamAccountName);
        var (username, password) = ExtractCredentials();
        _groupService.RemoveMember(samAccountName, memberSamAccountName, username, password);
        return NoContent();
    }
}
