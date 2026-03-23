using ACLS.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

/// <summary>
/// Abstract base controller. Every controller in ACLS.Api inherits from this.
/// Injects IMediator. [ApiController] enables automatic model validation and problem details.
/// [Authorize] applies globally — override with [AllowAnonymous] on specific actions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IMediator Mediator;

    protected ApiControllerBase(IMediator mediator)
    {
        Mediator = mediator;
    }

    /// <summary>
    /// Maps an Application-layer Error to the appropriate HTTP IActionResult with RFC 7807 Problem Details.
    /// errorCode values and HTTP status codes follow the catalogue in docs/05_API/error_codes.md exactly.
    /// </summary>
    protected IActionResult MapError(Error error)
    {
        var (statusCode, type) = error.Code switch
        {
            // 503 Service Unavailable
            "System.StorageUnavailable" or "System.DatabaseUnavailable" =>
                (StatusCodes.Status503ServiceUnavailable, "https://acls.api/errors/service-unavailable"),

            // 409 Conflict
            "Auth.EmailAlreadyRegistered" or "Auth.InvitationTokenAlreadyUsed"
                or "User.AlreadyDeactivated" or "User.AlreadyActive"
                or "Complaint.FeedbackAlreadySubmitted" =>
                (StatusCodes.Status409Conflict, "https://acls.api/errors/conflict"),

            // 404 Not Found — Complaint.AccessDenied returns 404, not 403 (multi-tenancy: do not reveal cross-property existence)
            var c when c.EndsWith(".NotFound") || c == "Complaint.AccessDenied" =>
                (StatusCodes.Status404NotFound, "https://acls.api/errors/not-found"),

            // 403 Forbidden — Auth.InsufficientRole and generic *.AccessDenied (non-Complaint)
            var c when c.EndsWith(".AccessDenied") || c == "Auth.InsufficientRole" =>
                (StatusCodes.Status403Forbidden, "https://acls.api/errors/forbidden"),

            // 401 Unauthorized
            var c when c.StartsWith("Auth.") =>
                (StatusCodes.Status401Unauthorized, "https://acls.api/errors/unauthorized"),

            // 422 Unprocessable Entity
            var c when c.EndsWith(".InvalidStatusTransition")
                    || c == "Staff.CannotSetBusyManually"
                    || c == "Complaint.AlreadyClosed"
                    || c == "Complaint.AlreadyResolved"
                    || c == "Complaint.NotAssigned"
                    || c == "Complaint.StaffNotAvailable"
                    || c == "Complaint.FeedbackNotAllowed"
                    || c == "User.CannotDeactivateSelf" =>
                (StatusCodes.Status422UnprocessableEntity, "https://acls.api/errors/unprocessable-entity"),

            // 400 Bad Request (FluentValidation)
            "Validation.Failed" =>
                (StatusCodes.Status400BadRequest, "https://acls.api/errors/validation"),

            // 400 Bad Request (default)
            _ => (StatusCodes.Status400BadRequest, "https://acls.api/errors/bad-request")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = error.Message,
            Instance = HttpContext.Request.Path
        };
        problemDetails.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        404 => "Resource not found",
        403 => "Access denied",
        401 => "Unauthorized",
        409 => "Conflict",
        422 => "Unprocessable entity",
        503 => "Service unavailable",
        400 => "Bad request",
        _ => "An error occurred"
    };
}
