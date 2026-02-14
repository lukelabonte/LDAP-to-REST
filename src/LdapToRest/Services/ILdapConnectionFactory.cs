namespace LdapToRest.Services;

using System.DirectoryServices.Protocols;

public interface ILdapConnectionFactory
{
    LdapConnection CreateConnection(string username, string password);
}
