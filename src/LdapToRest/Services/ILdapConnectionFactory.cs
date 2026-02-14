namespace LdapToRest.Services;

public interface ILdapConnectionFactory
{
    ILdapConnection CreateConnection(string username, string password);
}
