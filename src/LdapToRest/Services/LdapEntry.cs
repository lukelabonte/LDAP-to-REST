namespace LdapToRest.Services;

public class LdapEntry
{
    public string DistinguishedName { get; set; } = string.Empty;

    private readonly Dictionary<string, string[]> _attributes = new(StringComparer.OrdinalIgnoreCase);

    public string? GetAttribute(string name)
        => _attributes.TryGetValue(name, out var values) && values.Length > 0 ? values[0] : null;

    public string[]? GetMultiValueAttribute(string name)
        => _attributes.TryGetValue(name, out var values) ? values : null;

    public void SetAttribute(string name, params string[] values)
        => _attributes[name] = values;
}
