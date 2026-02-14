namespace LdapToRest.Configuration;

public class LdapSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string BaseDn { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string[] CorsAllowedOrigins { get; set; } = [];
}
