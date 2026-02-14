namespace LdapToRest.Services;

using System.DirectoryServices.Protocols;
using System.Net;
using LdapToRest.Configuration;

public class LdapConnectionFactory : ILdapConnectionFactory
{
    private readonly LdapSettings _settings;

    public LdapConnectionFactory(LdapSettings settings)
    {
        _settings = settings;
    }

    public LdapConnection CreateConnection(string username, string password)
    {
        var identifier = new LdapDirectoryIdentifier(_settings.Host, _settings.Port);
        var credential = new NetworkCredential(username, password);
        var connection = new LdapConnection(identifier, credential);
        connection.SessionOptions.ProtocolVersion = 3;

        if (_settings.UseSsl)
            connection.SessionOptions.SecureSocketLayer = true;

        connection.AuthType = AuthType.Basic;
        connection.Bind();
        return connection;
    }
}
