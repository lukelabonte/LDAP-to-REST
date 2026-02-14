using LdapToRest.Configuration;
using LdapToRest.Middleware;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration from environment variables ---
var ldapHost = Environment.GetEnvironmentVariable("LDAP_HOST")
    ?? throw new InvalidOperationException("LDAP_HOST environment variable is required.");
var ldapBaseDn = Environment.GetEnvironmentVariable("LDAP_BASE_DN")
    ?? throw new InvalidOperationException("LDAP_BASE_DN environment variable is required.");
var useSsl = bool.TryParse(Environment.GetEnvironmentVariable("LDAP_USE_SSL"), out var ssl) && ssl;
var defaultPort = useSsl ? 636 : 389;
var ldapPort = int.TryParse(Environment.GetEnvironmentVariable("LDAP_PORT"), out var port) ? port : defaultPort;

var ldapSettings = new LdapSettings
{
    Host = ldapHost,
    Port = ldapPort,
    BaseDn = ldapBaseDn,
    UseSsl = useSsl,
    CorsAllowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? []
};

// --- Services ---
builder.Services.AddSingleton(ldapSettings);
builder.Services.AddScoped<ILdapConnectionFactory, LdapConnectionFactory>();
builder.Services.AddScoped<ILdapUserService, LdapUserService>();
builder.Services.AddScoped<ILdapGroupService, LdapGroupService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Authentication ---
builder.Services.AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// --- CORS ---
if (ldapSettings.CorsAllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(ldapSettings.CorsAllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

// --- Middleware pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

if (ldapSettings.CorsAllowedOrigins.Length > 0)
    app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- Health check (no auth required) ---
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.Run();
