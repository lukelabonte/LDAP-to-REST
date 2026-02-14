namespace LdapToRest.Middleware;

using System.DirectoryServices.Protocols;
using LdapToRest.Models;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP operation failed with error code {ErrorCode}", ex.ErrorCode);

            var (statusCode, message) = ex.ErrorCode switch
            {
                49 => (401, "Invalid credentials"),
                32 => (404, "Object not found in directory"),
                50 => (403, "Insufficient access rights"),
                53 => (409, "Server unwilling to perform operation"),
                68 => (409, "Entry already exists"),
                -1 or 81 or 82 or 85 or 91 => (502, "Unable to reach Active Directory server"),
                _ => (500, "LDAP operation failed")
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Status = statusCode,
                Message = message,
                Detail = ex.Message
            });
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "LDAP directory operation failed");

            // Parse AD error codes from the message (e.g., "000004DC" = bind required)
            var statusCode = 500;
            var message = "LDAP operation failed";

            if (ex.Message.Contains("000004DC") || ex.Message.Contains("successful bind must be completed"))
            {
                statusCode = 401;
                message = "Authentication failed — LDAP bind was not completed";
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Status = statusCode,
                Message = message,
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Status = 400,
                Message = "Invalid request",
                Detail = ex.Message
            });
        }
    }
}
