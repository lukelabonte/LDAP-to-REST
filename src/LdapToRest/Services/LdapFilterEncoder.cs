namespace LdapToRest.Services;

using System.Text;

/// <summary>
/// Encodes values for LDAP search filters per RFC 4515.
/// Layer 2 of 3-layer injection prevention.
/// </summary>
public static class LdapFilterEncoder
{
    public static string Encode(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '*':  sb.Append("\\2a"); break;
                case '(':  sb.Append("\\28"); break;
                case ')':  sb.Append("\\29"); break;
                case '\\': sb.Append("\\5c"); break;
                case '\0': sb.Append("\\00"); break;
                default:   sb.Append(c);      break;
            }
        }
        return sb.ToString();
    }
}
