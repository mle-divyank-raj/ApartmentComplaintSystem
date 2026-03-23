using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns RFC 7807 Problem Details with status 500.
/// Internal exception details are logged but never exposed to the client.
/// Must be registered FIRST in the middleware pipeline.
/// </summary>
public sealed class ExceptionHandlingMiddleware
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemDetailsAsync(context, ex);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problem = new
        {
            type = "https://acls.api/errors/internal-server-error",
            title = "An unexpected error occurred",
            status = 500,
            detail = "An internal server error occurred. Please try again later.",
            instance = context.Request.Path.Value,
            errorCode = "System.InternalError"
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
