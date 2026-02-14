namespace LdapToRest.Controllers;

using System.Text.Json;
using LdapToRest.Middleware;
using Microsoft.AspNetCore.Mvc;

public abstract class LdapApiController : ControllerBase
{
    protected (string Username, string Password) ExtractCredentials()
    {
        var username = HttpContext.Items[BasicAuthenticationHandler.UsernameKey] as string
                       ?? throw new InvalidOperationException("Username not found in context");
        var password = HttpContext.Items[BasicAuthenticationHandler.PasswordKey] as string
                       ?? throw new InvalidOperationException("Password not found in context");
        return (username, password);
    }

    protected static Dictionary<string, object?> ParseAndValidateModifications(
        JsonElement body, HashSet<string> allowedAttributes)
    {
        var modifications = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in body.EnumerateObject())
        {
            var key = property.Name.ToLowerInvariant();
            if (!allowedAttributes.Contains(key))
                throw new ArgumentException($"Attribute '{property.Name}' is not modifiable");
            modifications[key] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => throw new ArgumentException($"Unsupported value type for '{property.Name}'")
            };
        }
        return modifications;
    }
}
