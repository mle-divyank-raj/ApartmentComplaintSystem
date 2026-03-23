using ACLS.Application.Users.Commands.LoginUser;
using ACLS.Application.Users.Commands.RegisterResident;
using ACLS.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

/// <summary>
/// Handles authentication: resident registration and login.
/// Both endpoints are public ([AllowAnonymous]) — no JWT required.
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    public AuthController(IMediator mediator) : base(mediator) { }

    /// <summary>POST /api/v1/auth/register — Register a new Resident account using an invitation token.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterResidentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterResidentCommand(
            request.InvitationToken,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Phone);

        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>POST /api/v1/auth/login — Authenticate and receive a JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await Mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }
}
