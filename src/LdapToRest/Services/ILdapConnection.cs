namespace LdapToRest.Services;

using System.DirectoryServices.Protocols;

public interface ILdapConnection : IDisposable
{
    List<LdapEntry> Search(string baseDn, string filter, string[] attributes, SearchScope scope);
    void Modify(string distinguishedName, params DirectoryAttributeModification[] modifications);
}
