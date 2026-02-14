namespace LdapToRest.Tests.Services;

using LdapToRest.Services;

public class LdapFilterEncoderTests
{
    [Fact]
    public void Encode_PlainString_ReturnsUnchanged()
    {
        Assert.Equal("jsmith", LdapFilterEncoder.Encode("jsmith"));
    }

    [Fact]
    public void Encode_Asterisk_EscapesCorrectly()
    {
        Assert.Equal("john\\2a", LdapFilterEncoder.Encode("john*"));
    }

    [Fact]
    public void Encode_Parentheses_EscapesCorrectly()
    {
        Assert.Equal("\\28admin\\29", LdapFilterEncoder.Encode("(admin)"));
    }

    [Fact]
    public void Encode_Backslash_EscapesCorrectly()
    {
        Assert.Equal("a\\5cb", LdapFilterEncoder.Encode("a\\b"));
    }

    [Fact]
    public void Encode_NullChar_EscapesCorrectly()
    {
        Assert.Equal("a\\00b", LdapFilterEncoder.Encode("a\0b"));
    }

    [Fact]
    public void Encode_AllSpecialChars_EscapesAll()
    {
        Assert.Equal("\\2a\\28\\29\\5c\\00", LdapFilterEncoder.Encode("*()\\\0"));
    }

    [Fact]
    public void Encode_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", LdapFilterEncoder.Encode(""));
    }

    [Fact]
    public void Encode_InjectionAttempt_Neutralized()
    {
        var input = "admin)(|(objectClass=*)";
        var result = LdapFilterEncoder.Encode(input);
        Assert.DoesNotContain(")(", result);
        Assert.DoesNotContain("*)", result);
    }
}
