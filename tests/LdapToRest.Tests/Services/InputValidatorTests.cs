namespace LdapToRest.Tests.Services;

using LdapToRest.Services;

public class InputValidatorTests
{
    // --- SamAccountName validation ---
    [Fact]
    public void ValidateSamAccountName_ValidName_DoesNotThrow()
    {
        InputValidator.ValidateSamAccountName("jsmith");  // Should not throw
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateSamAccountName_EmptyOrNull_ThrowsArgumentException(string? value)
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateSamAccountName(value!));
    }

    [Fact]
    public void ValidateSamAccountName_TooLong_ThrowsArgumentException()
    {
        var longName = new string('a', 257);
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateSamAccountName(longName));
    }

    [Theory]
    [InlineData("user\"name")]
    [InlineData("user[name")]
    [InlineData("user]name")]
    [InlineData("user:name")]
    [InlineData("user;name")]
    [InlineData("user|name")]
    [InlineData("user=name")]
    [InlineData("user+name")]
    [InlineData("user*name")]
    [InlineData("user?name")]
    [InlineData("user<name")]
    [InlineData("user>name")]
    [InlineData("user/name")]
    [InlineData("user\\name")]
    [InlineData("user,name")]
    public void ValidateSamAccountName_DisallowedChars_ThrowsArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateSamAccountName(value));
    }

    [Fact]
    public void ValidateSamAccountName_TrailingPeriod_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateSamAccountName("jsmith."));
    }

    [Fact]
    public void ValidateSamAccountName_PeriodNotTrailing_DoesNotThrow()
    {
        InputValidator.ValidateSamAccountName("j.smith");  // Should not throw
    }

    [Fact]
    public void ValidateSamAccountName_NonPrintableChar_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateSamAccountName("user\x01name"));
    }

    // --- DN validation ---
    [Fact]
    public void ValidateDistinguishedName_ValidDn_DoesNotThrow()
    {
        InputValidator.ValidateDistinguishedName("CN=John Smith,OU=Users,DC=example,DC=com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateDistinguishedName_EmptyOrNull_ThrowsArgumentException(string? value)
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateDistinguishedName(value!));
    }

    [Fact]
    public void ValidateDistinguishedName_NullBytes_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InputValidator.ValidateDistinguishedName("CN=user\0,DC=test"));
    }
}
