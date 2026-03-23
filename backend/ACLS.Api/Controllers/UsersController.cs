using ACLS.Application.Users.Commands.DeactivateUser;
using ACLS.Application.Users.Commands.InviteResident;
using ACLS.Application.Users.Commands.ReactivateUser;
using ACLS.Application.Users.Queries.GetAllUsers;
using ACLS.Contracts.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

[Route("api/v1/users")]
[Authorize(Roles = "Manager")]
public sealed class UsersController : ApiControllerBase
{
    public UsersController(IMediator mediator) : base(mediator) { }

    // ── GET /api/v1/users ────────────────────────────────────────────────────

    /// <summary>Get all users for the property (Manager only).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAllUsersQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/users/invite ────────────────────────────────────────────

    /// <summary>Invite a resident to register (Manager only). Returns the invitation token.</summary>
    [HttpPost("invite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Invite(
        [FromBody] InviteResidentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new InviteResidentCommand(request.Email, request.UnitId), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // ── POST /api/v1/users/{userId}/deactivate ───────────────────────────────

    /// <summary>Deactivate a user account (Manager only, same property).</summary>
    [HttpPost("{userId:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        int userId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeactivateUserCommand(userId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/users/{userId}/reactivate ───────────────────────────────

    /// <summary>Reactivate a previously deactivated user account (Manager only, same property).</summary>
    [HttpPost("{userId:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(
        int userId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ReactivateUserCommand(userId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }
}
