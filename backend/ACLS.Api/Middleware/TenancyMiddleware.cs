using System.Text.Json;
using ACLS.Api.Services;
using ACLS.Application.Common.Interfaces;

namespace ACLS.Api.Middleware;

/// <summary>
/// Extracts PropertyId and UserId from the authenticated user's JWT claims and
/// populates the scoped CurrentPropertyContext. Runs AFTER UseAuthentication().
///
/// Anonymous endpoints (AllowAnonymous) pass through without context population,
/// since HttpContext.User.Identity.IsAuthenticated will be false.
///
/// Returns 401 with errorCode "Auth.MissingPropertyClaim" if the token is present but
/// lacks the required claims.
/// </summary>
public sealed class TenancyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenancyMiddleware> _logger;

    public TenancyMiddleware(RequestDelegate next, ILogger<TenancyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentPropertyContext propertyContext)
    {
        // Pass through for unauthenticated requests (anonymous endpoints such as /auth/login)
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var subClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("sub");

        var propertyIdClaim = context.User.FindFirst("property_id");

        if (subClaim is null || propertyIdClaim is null
            || !int.TryParse(subClaim.Value, out var userId)
            || !int.TryParse(propertyIdClaim.Value, out var propertyId))
        {
            _logger.LogWarning("Authenticated request missing required JWT claims (sub or property_id).");
            await WriteMissingClaimsResponseAsync(context);
            return;
        }

        ((CurrentPropertyContext)propertyContext).SetContext(propertyId, userId);

        await _next(context);
    }

    private static async Task WriteMissingClaimsResponseAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://acls.api/errors/missing-property-claim",
            title = "Missing required JWT claims",
            status = 401,
            detail = "The JWT token is missing the required 'sub' or 'property_id' claims.",
            instance = context.Request.Path.Value,
            errorCode = "Auth.MissingPropertyClaim"
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
