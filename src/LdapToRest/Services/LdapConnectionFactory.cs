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

    public ILdapConnection CreateConnection(string username, string password)
    {
        var identifier = new LdapDirectoryIdentifier(_settings.Host, _settings.Port);
        var credential = new NetworkCredential(username, password);
        var connection = new LdapConnection(identifier, credential, AuthType.Basic);
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        connection.AutoBind = false;

        if (_settings.IgnoreCertErrors)
            connection.SessionOptions.VerifyServerCertificate = (_, _) => true;

        if (_settings.UseSsl)
            connection.SessionOptions.SecureSocketLayer = true;
        else if (_settings.StartTls)
            connection.SessionOptions.StartTransportLayerSecurity(null);

        connection.Bind(credential);
        return new LdapConnectionAdapter(connection);
    }
}

internal class LdapConnectionAdapter : ILdapConnection
{
    private readonly LdapConnection _connection;

    public LdapConnectionAdapter(LdapConnection connection)
    {
        _connection = connection;
    }

    public List<LdapEntry> Search(string baseDn, string filter, string[] attributes, SearchScope scope)
    {
        var request = new SearchRequest(baseDn, filter, scope, attributes);
        var response = (SearchResponse)_connection.SendRequest(request);

        var entries = new List<LdapEntry>();
        foreach (SearchResultEntry entry in response.Entries)
        {
            var ldapEntry = new LdapEntry { DistinguishedName = entry.DistinguishedName };
            foreach (DirectoryAttribute attr in entry.Attributes.Values)
            {
                var values = attr.GetValues(typeof(string)).Cast<string>().ToArray();
                ldapEntry.SetAttribute(attr.Name, values);
            }
            entries.Add(ldapEntry);
        }
        return entries;
    }

    public void Modify(string distinguishedName, params DirectoryAttributeModification[] modifications)
    {
        var request = new ModifyRequest(distinguishedName, modifications);
        _connection.SendRequest(request);
    }

    public void Dispose() => _connection.Dispose();
}
