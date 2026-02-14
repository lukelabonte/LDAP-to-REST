namespace LdapToRest.Controllers;

using System.Text.Json;
using LdapToRest.Models;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manage Active Directory groups. Look up groups, modify attributes, and manage group membership
/// (list members, check if a user is a member, add/remove members).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
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

    /// <summary>
    /// Get a group by its SamAccountName (AD login name)
    /// </summary>
    /// <remarks>
    /// Looks up a group in Active Directory by its SamAccountName — the short name
    /// (e.g., `developers`, `domain-admins`, `vpn-users`).
    ///
    /// Returns group attributes including description, who manages the group,
    /// what groups it belongs to, and a list of direct member DNs.
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`). Max 256 characters, no special characters like `* ? \ / &lt; &gt;`.</param>
    /// <returns>The group's AD attributes</returns>
    /// <response code="200">Group found — returns all attributes</response>
    /// <response code="400">Invalid SamAccountName (empty, too long, or contains disallowed characters)</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="404">No group with that SamAccountName exists in AD</response>
    [HttpGet("{samAccountName}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetBySamAccountName(string samAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var (username, password) = ExtractCredentials();
        var group = _groupService.FindBySamAccountName(samAccountName, username, password);
        return group == null ? NotFound() : Ok(group);
    }

    /// <summary>
    /// Get a group by its Distinguished Name (full LDAP path)
    /// </summary>
    /// <remarks>
    /// Looks up a group by its full Distinguished Name in the directory tree.
    /// Use this when you have the exact DN, for example from a `memberOf` field on a user.
    ///
    /// Example DN: `CN=Developers,OU=Groups,DC=example,DC=com`
    /// </remarks>
    /// <param name="distinguishedName">Full LDAP path (e.g., `CN=Developers,OU=Groups,DC=example,DC=com`)</param>
    /// <returns>The group's AD attributes</returns>
    /// <response code="200">Group found — returns all attributes</response>
    /// <response code="400">Invalid or empty Distinguished Name</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="404">No group at that DN</response>
    [HttpGet("dn/{*distinguishedName}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetByDistinguishedName(string distinguishedName)
    {
        InputValidator.ValidateDistinguishedName(distinguishedName);
        var (username, password) = ExtractCredentials();
        var group = _groupService.FindByDistinguishedName(distinguishedName, username, password);
        return group == null ? NotFound() : Ok(group);
    }

    /// <summary>
    /// Update a group's attributes
    /// </summary>
    /// <remarks>
    /// Modifies one or more attributes on an AD group. Only the attributes listed below
    /// can be changed — attempting to modify any other attribute returns 400.
    ///
    /// **Modifiable attributes:**
    /// - `description` — Free-text description of the group's purpose
    /// - `displayname` — Display name shown in the address book
    ///
    /// **Setting a value to `null`** removes that attribute from the group.
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`)</param>
    /// <param name="body">JSON object with attribute names as keys. Values can be strings or `null` to remove.</param>
    /// <response code="204">Attributes updated successfully</response>
    /// <response code="400">Invalid SamAccountName, unsupported attribute, or malformed body</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="403">Your AD account lacks permission to modify this group</response>
    /// <response code="404">No group with that SamAccountName exists</response>
    [HttpPatch("{samAccountName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult Update(string samAccountName, [FromBody] JsonElement body)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var modifications = ParseAndValidateModifications(body, AllowedWriteAttributes);
        var (username, password) = ExtractCredentials();
        _groupService.UpdateGroup(samAccountName, modifications, username, password);
        return NoContent();
    }

    /// <summary>
    /// List group members (paginated)
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of the group's members with their SamAccountName, display name,
    /// and whether they are a user or nested group.
    ///
    /// **Non-recursive (default):** Only returns direct members of the group.
    ///
    /// **Recursive (`recursive=true`):** Returns all members including those in nested groups.
    /// Uses AD's `LDAP_MATCHING_RULE_IN_CHAIN` (OID 1.2.840.113556.1.4.1941) for efficient
    /// server-side recursive resolution — no client-side recursion needed.
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`)</param>
    /// <param name="recursive">If `true`, include members of nested groups. Default: `false` (direct members only).</param>
    /// <param name="page">Page number (1-based). Default: `1`.</param>
    /// <param name="pageSize">Number of members per page. Default: `50`. Max recommended: `500`.</param>
    /// <returns>Paginated list of group members</returns>
    /// <response code="200">Returns paginated member list with total count and page info</response>
    /// <response code="400">Invalid SamAccountName</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="404">No group with that SamAccountName exists</response>
    [HttpGet("{samAccountName}/members")]
    [ProducesResponseType(typeof(PaginatedResult<GroupMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Check if a user is a member of a group
    /// </summary>
    /// <remarks>
    /// Checks whether the specified user is a member of the group (including nested/transitive membership).
    ///
    /// Returns a result object indicating membership status along with the resolved
    /// Distinguished Names of both the member and the group.
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`)</param>
    /// <param name="memberSamAccountName">The user's AD login name to check (e.g., `jsmith`)</param>
    /// <returns>Membership check result</returns>
    /// <response code="200">Returns membership status (`isMember: true/false`)</response>
    /// <response code="400">Invalid SamAccountName for group or member</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    [HttpGet("{samAccountName}/members/{memberSamAccountName}")]
    [ProducesResponseType(typeof(MembershipCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult CheckMembership(string samAccountName, string memberSamAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateSamAccountName(memberSamAccountName);
        var (username, password) = ExtractCredentials();
        var result = _groupService.CheckMembership(samAccountName, memberSamAccountName, username, password);
        return Ok(result);
    }

    /// <summary>
    /// Add a member to a group
    /// </summary>
    /// <remarks>
    /// Adds a user or group to this group's membership list. The member must be specified
    /// by their full Distinguished Name (DN).
    ///
    /// **Where to get the DN:** Use the `GET /api/users/{samAccountName}` endpoint first —
    /// the `distinguishedName` field in the response is what you need here.
    ///
    /// **Example:**
    /// ```json
    /// {
    ///   "distinguishedName": "CN=John Smith,OU=Users,DC=example,DC=com"
    /// }
    /// ```
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`)</param>
    /// <param name="request">The DN of the user or group to add</param>
    /// <response code="201">Member added successfully</response>
    /// <response code="400">Invalid SamAccountName or DN</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="403">Your AD account lacks permission to modify this group's membership</response>
    /// <response code="409">Member is already in the group, or another conflict</response>
    [HttpPost("{samAccountName}/members")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public IActionResult AddMember(string samAccountName, [FromBody] AddGroupMemberRequest request)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateDistinguishedName(request.DistinguishedName);
        var (username, password) = ExtractCredentials();
        _groupService.AddMember(samAccountName, request.DistinguishedName, username, password);
        return StatusCode(201);
    }

    /// <summary>
    /// Remove a member from a group
    /// </summary>
    /// <remarks>
    /// Removes a user or group from this group's membership list.
    /// The member is identified by their SamAccountName — the API resolves their DN internally.
    /// </remarks>
    /// <param name="samAccountName">The group's AD name (e.g., `developers`)</param>
    /// <param name="memberSamAccountName">The SamAccountName of the member to remove (e.g., `jsmith`)</param>
    /// <response code="204">Member removed successfully</response>
    /// <response code="400">Invalid SamAccountName for group or member</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="403">Your AD account lacks permission to modify this group's membership</response>
    /// <response code="404">Member not found in AD</response>
    [HttpDelete("{samAccountName}/members/{memberSamAccountName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult RemoveMember(string samAccountName, string memberSamAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        InputValidator.ValidateSamAccountName(memberSamAccountName);
        var (username, password) = ExtractCredentials();
        _groupService.RemoveMember(samAccountName, memberSamAccountName, username, password);
        return NoContent();
    }
}
