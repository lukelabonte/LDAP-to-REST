namespace LdapToRest.Services;

/// <summary>
/// Validates user input before it reaches LDAP operations.
/// Layer 1 of 3-layer injection prevention.
/// </summary>
public static class InputValidator
{
    // Characters disallowed in sAMAccountName per Microsoft AD docs
    private static readonly HashSet<char> DisallowedSamChars = new()
    {
        '"', '[', ']', ':', ';', '|', '=', '+', '*', '?', '<', '>', '/', '\\', ','
    };

    private const int MaxSamAccountNameLength = 256;

    public static void ValidateSamAccountName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SamAccountName cannot be empty.");

        if (value.Length > MaxSamAccountNameLength)
            throw new ArgumentException($"SamAccountName cannot exceed {MaxSamAccountNameLength} characters.");

        if (value.EndsWith('.'))
            throw new ArgumentException("SamAccountName cannot end with a period.");

        foreach (var c in value)
        {
            if (c < 32 || c == 127)
                throw new ArgumentException($"SamAccountName contains non-printable character (0x{(int)c:X2}).");

            if (DisallowedSamChars.Contains(c))
                throw new ArgumentException($"SamAccountName contains disallowed character '{c}'.");
        }
    }

    public static void ValidateDistinguishedName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Distinguished name cannot be empty.");

        if (value.Contains('\0'))
            throw new ArgumentException("Distinguished name contains null bytes.");
    }
}
