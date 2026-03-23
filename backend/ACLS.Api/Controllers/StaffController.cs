using ACLS.Application.Staff.Commands.UpdateAvailability;
using ACLS.Application.Staff.Queries.GetAllStaff;
using ACLS.Application.Staff.Queries.GetAvailableStaff;
using ACLS.Application.Staff.Queries.GetMyStaffProfile;
using ACLS.Application.Staff.Queries.GetStaffById;
using ACLS.Contracts.Staff;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

[Route("api/v1/staff")]
public sealed class StaffController : ApiControllerBase
{
    public StaffController(IMediator mediator) : base(mediator) { }

    // ── GET /api/v1/staff/me ─────────────────────────────────────────────────
    // IMPORTANT: Must be declared BEFORE GET /{staffMemberId} to prevent route ambiguity.

    /// <summary>Get the profile and active assignments for the authenticated maintenance staff member.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        // UserId is resolved from ICurrentPropertyContext inside the handler
        var result = await Mediator.Send(new GetMyStaffProfileQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/staff/available ──────────────────────────────────────────
    // IMPORTANT: Must be declared BEFORE GET /{staffMemberId} to prevent route ambiguity.

    /// <summary>Get all staff members currently in an AVAILABLE state (Manager only).</summary>
    [HttpGet("available")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAvailableStaffQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/staff ────────────────────────────────────────────────────

    /// <summary>Get all staff members for the property (Manager only).</summary>
    [HttpGet]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAllStaffQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/staff/{staffMemberId} ────────────────────────────────────

    /// <summary>Get a specific staff member by ID (Manager only).</summary>
    [HttpGet("{staffMemberId:int}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int staffMemberId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStaffByIdQuery(staffMemberId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/staff/{staffMemberId}/availability ──────────────────────

    /// <summary>Update staff member availability (MaintenanceStaff only, own record).</summary>
    [HttpPost("{staffMemberId:int}/availability")]
    [Authorize(Roles = "MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAvailability(
        int staffMemberId,
        [FromBody] UpdateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateAvailabilityCommand(staffMemberId, request.Availability), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }
}
