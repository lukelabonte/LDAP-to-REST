using LdapToRest.Configuration;
using LdapToRest.Middleware;
using LdapToRest.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration from environment variables ---
var ldapHost = Environment.GetEnvironmentVariable("LDAP_HOST")
    ?? throw new InvalidOperationException("LDAP_HOST environment variable is required.");
var ldapBaseDn = Environment.GetEnvironmentVariable("LDAP_BASE_DN")
    ?? throw new InvalidOperationException("LDAP_BASE_DN environment variable is required.");
var useSsl = bool.TryParse(Environment.GetEnvironmentVariable("LDAP_USE_SSL"), out var ssl) && ssl;
var defaultPort = useSsl ? 636 : 389;
var ldapPort = int.TryParse(Environment.GetEnvironmentVariable("LDAP_PORT"), out var port) ? port : defaultPort;

var startTls = bool.TryParse(Environment.GetEnvironmentVariable("LDAP_START_TLS"), out var tls) && tls; // default false
var ignoreCertErrors = bool.TryParse(Environment.GetEnvironmentVariable("LDAP_IGNORE_CERT_ERRORS"), out var ignoreCert) && ignoreCert;

// On Linux, OpenLDAP's libldap ignores .NET's VerifyServerCertificate callback for StartTLS.
// It reads LDAPTLS_REQCERT instead. Set it so LDAP_IGNORE_CERT_ERRORS works cross-platform.
if (ignoreCertErrors)
    Environment.SetEnvironmentVariable("LDAPTLS_REQCERT", "never");

var ldapSettings = new LdapSettings
{
    Host = ldapHost,
    Port = ldapPort,
    BaseDn = ldapBaseDn,
    UseSsl = useSsl,
    StartTls = !useSsl && startTls, // StartTLS only applies when not using LDAPS
    IgnoreCertErrors = ignoreCertErrors,
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
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Active Directory credentials (DOMAIN\\username and password)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
