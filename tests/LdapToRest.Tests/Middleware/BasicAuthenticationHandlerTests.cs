namespace LdapToRest.Tests.Middleware;

using System.Net.Http.Headers;
using System.Text;
using LdapToRest.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

public class BasicAuthenticationHandlerTests
{
    private async Task<AuthenticateResult> RunHandler(HttpContext context)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(o => o.Get(BasicAuthenticationHandler.SchemeName))
               .Returns(new AuthenticationSchemeOptions());

        var loggerFactory = new NullLoggerFactory();
        var encoder = new UrlTestEncoder();

        var handler = new BasicAuthenticationHandler(options.Object, loggerFactory, encoder);

        var scheme = new AuthenticationScheme(
            BasicAuthenticationHandler.SchemeName,
            null,
            typeof(BasicAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    private static HttpContext CreateContext(string? authorizationHeader = null)
    {
        var context = new DefaultHttpContext();
        if (authorizationHeader != null)
            context.Request.Headers.Authorization = authorizationHeader;
        return context;
    }

    private static string EncodeBasicAuth(string username, string password)
    {
        var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
        return $"Basic {Convert.ToBase64String(bytes)}";
    }

    [Fact]
    public async Task MissingAuthorizationHeader_ReturnsFailure()
    {
        var context = CreateContext();
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task WrongScheme_ReturnsFailure()
    {
        var context = CreateContext("Bearer some-token");
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task InvalidBase64_ReturnsFailure()
    {
        var context = CreateContext("Basic not-valid-base64!!!");
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task MissingPassword_ReturnsFailure()
    {
        var bytes = Encoding.UTF8.GetBytes("usernameonly");
        var context = CreateContext($"Basic {Convert.ToBase64String(bytes)}");
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task EmptyUsername_ReturnsFailure()
    {
        var context = CreateContext(EncodeBasicAuth("", "password"));
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task EmptyPassword_ReturnsFailure()
    {
        var context = CreateContext(EncodeBasicAuth("user", ""));
        var result = await RunHandler(context);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidCredentials_ReturnsSuccess()
    {
        var context = CreateContext(EncodeBasicAuth("admin", "s3cret"));
        var result = await RunHandler(context);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidCredentials_StoresUsernameInContext()
    {
        var context = CreateContext(EncodeBasicAuth("admin", "s3cret"));
        await RunHandler(context);
        Assert.Equal("admin", context.Items[BasicAuthenticationHandler.UsernameKey]);
    }

    [Fact]
    public async Task ValidCredentials_StoresPasswordInContext()
    {
        var context = CreateContext(EncodeBasicAuth("admin", "s3cret"));
        await RunHandler(context);
        Assert.Equal("s3cret", context.Items[BasicAuthenticationHandler.PasswordKey]);
    }

    [Fact]
    public async Task ValidCredentials_SetsClaimsIdentity()
    {
        var context = CreateContext(EncodeBasicAuth("admin", "s3cret"));
        var result = await RunHandler(context);
        Assert.Equal("admin", result.Principal?.Identity?.Name);
    }

    [Fact]
    public async Task PasswordWithColon_ParsedCorrectly()
    {
        // Passwords can contain colons — split on first colon only
        var context = CreateContext(EncodeBasicAuth("admin", "pass:with:colons"));
        var result = await RunHandler(context);
        Assert.True(result.Succeeded);
        Assert.Equal("pass:with:colons", context.Items[BasicAuthenticationHandler.PasswordKey]);
    }
}
