namespace LdapToRest.Controllers;

using System.Text.Json;
using LdapToRest.Models;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Manage Active Directory user accounts. Look up users by their login name or full LDAP path,
/// and modify attributes like department, title, and enabled status.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
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

    /// <summary>
    /// Get a user by their SamAccountName (AD login name)
    /// </summary>
    /// <remarks>
    /// Looks up a user in Active Directory by their SamAccountName — the short login name
    /// (e.g., `jsmith`, `admin`, `mack-plex`). This is the same name used to log in to Windows.
    ///
    /// Returns all standard AD attributes for the user including group memberships,
    /// email, manager, and whether the account is enabled or disabled.
    /// </remarks>
    /// <param name="samAccountName">The user's AD login name (e.g., `jsmith`). Max 256 characters, no special characters like `* ? \ / &lt; &gt;`.</param>
    /// <returns>The user's AD attributes</returns>
    /// <response code="200">User found — returns all attributes</response>
    /// <response code="400">Invalid SamAccountName (empty, too long, or contains disallowed characters)</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="404">No user with that SamAccountName exists in AD</response>
    [HttpGet("{samAccountName}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetBySamAccountName(string samAccountName)
    {
        InputValidator.ValidateSamAccountName(samAccountName);
        var (username, password) = ExtractCredentials();
        var user = _userService.FindBySamAccountName(samAccountName, username, password);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Get a user by their Distinguished Name (full LDAP path)
    /// </summary>
    /// <remarks>
    /// Looks up a user by their full Distinguished Name in the directory tree.
    /// Use this when you have the exact DN, for example from a `memberOf` or `manager` field.
    ///
    /// Example DN: `CN=John Smith,OU=Users,DC=example,DC=com`
    ///
    /// **Note:** The DN may contain slashes and commas — URL-encode them if needed
    /// (e.g., `CN=John%20Smith,OU=Users,DC=example,DC=com`).
    /// </remarks>
    /// <param name="distinguishedName">Full LDAP path (e.g., `CN=John Smith,OU=Users,DC=example,DC=com`)</param>
    /// <returns>The user's AD attributes</returns>
    /// <response code="200">User found — returns all attributes</response>
    /// <response code="400">Invalid or empty Distinguished Name</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="404">No user at that DN</response>
    [HttpGet("dn/{*distinguishedName}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetByDistinguishedName(string distinguishedName)
    {
        InputValidator.ValidateDistinguishedName(distinguishedName);
        var (username, password) = ExtractCredentials();
        var user = _userService.FindByDistinguishedName(distinguishedName, username, password);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Update a user's attributes
    /// </summary>
    /// <remarks>
    /// Modifies one or more attributes on an AD user account. Only the attributes listed below
    /// can be changed — attempting to modify any other attribute returns 400.
    ///
    /// **Modifiable attributes:**
    /// - `company` — Company name
    /// - `department` — Department name
    /// - `description` — Free-text description
    /// - `displayname` — Display name shown in the address book
    /// - `givenname` — First name
    /// - `enabled` — `true` to enable the account, `false` to disable it (sets the ACCOUNTDISABLE bit in userAccountControl)
    /// - `sn` — Last name (surname)
    ///
    /// **Setting a value to `null`** removes that attribute from the user.
    ///
    /// **Example body:**
    /// ```json
    /// {
    ///   "department": "Engineering",
    ///   "title": null
    /// }
    /// ```
    /// </remarks>
    /// <param name="samAccountName">The user's AD login name (e.g., `jsmith`)</param>
    /// <param name="body">JSON object with attribute names as keys. Values can be strings, booleans (for `enabled`), or `null` to remove.</param>
    /// <response code="204">Attributes updated successfully</response>
    /// <response code="400">Invalid SamAccountName, unsupported attribute, or malformed body</response>
    /// <response code="401">Missing or invalid AD credentials</response>
    /// <response code="403">Your AD account lacks permission to modify this user</response>
    /// <response code="404">No user with that SamAccountName exists</response>
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
        _userService.UpdateUser(samAccountName, modifications, username, password);
        return NoContent();
    }
}
