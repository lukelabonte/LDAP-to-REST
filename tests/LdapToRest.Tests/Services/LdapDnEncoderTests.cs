namespace LdapToRest.Tests.Services;

using LdapToRest.Services;

public class LdapDnEncoderTests
{
    [Fact]
    public void EncodeForFilter_PlainDn_ReturnsUnchanged()
    {
        Assert.Equal("CN=John,OU=Users,DC=example,DC=com",
            LdapDnEncoder.EncodeForFilter("CN=John,OU=Users,DC=example,DC=com"));
    }

    [Fact]
    public void EncodeForFilter_Parentheses_Escaped()
    {
        var result = LdapDnEncoder.EncodeForFilter("CN=Test (Group),DC=example,DC=com");
        Assert.Contains("\\28", result);
        Assert.Contains("\\29", result);
    }

    [Fact]
    public void EncodeForFilter_Asterisk_Escaped()
    {
        var result = LdapDnEncoder.EncodeForFilter("CN=All*Users,DC=example,DC=com");
        Assert.Contains("\\2a", result);
    }

    [Fact]
    public void EncodeForFilter_NullChar_Escaped()
    {
        var result = LdapDnEncoder.EncodeForFilter("CN=test\0user,DC=example,DC=com");
        Assert.Contains("\\00", result);
    }

    [Fact]
    public void EncodeForFilter_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", LdapDnEncoder.EncodeForFilter(""));
    }
}
