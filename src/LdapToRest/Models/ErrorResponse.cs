namespace LdapToRest.Models;

/// <summary>
/// Error response returned when an API request fails.
/// </summary>
public class ErrorResponse
{
    /// <summary>HTTP status code (e.g., 400, 401, 404, 500)</summary>
    public int Status { get; set; }

    /// <summary>Human-readable error message describing what went wrong</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Additional technical detail about the error (e.g., the underlying LDAP error message)</summary>
    public string? Detail { get; set; }
}
