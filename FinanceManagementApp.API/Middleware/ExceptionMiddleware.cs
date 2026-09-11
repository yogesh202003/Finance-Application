using System.Net;
using System.Text.Json;

namespace FinanceManagementApp.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var payload = JsonSerializer.Serialize(new
        {
            success = false,
            message,
            errors = Array.Empty<string>()
        });
        await context.Response.WriteAsync(payload);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var id = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var userId) ? userId : 0;
    }

    public static int? GetEmployeeId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var id = user.FindFirst("employeeId")?.Value;
        return int.TryParse(id, out var employeeId) ? employeeId : null;
    }

    public static string GetRole(this System.Security.Claims.ClaimsPrincipal user) =>
        user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
}
