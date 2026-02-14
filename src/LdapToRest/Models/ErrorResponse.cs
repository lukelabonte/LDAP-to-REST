namespace LdapToRest.Models;

public class ErrorResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
