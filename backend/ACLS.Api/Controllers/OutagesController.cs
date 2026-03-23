using ACLS.Application.Outages.Commands.DeclareOutage;
using ACLS.Application.Outages.Queries.GetAllOutages;
using ACLS.Application.Outages.Queries.GetOutageById;
using ACLS.Contracts.Outages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

[Route("api/v1/outages")]
public sealed class OutagesController : ApiControllerBase
{
    public OutagesController(IMediator mediator) : base(mediator) { }

    // ── POST /api/v1/outages ─────────────────────────────────────────────────

    /// <summary>Declare a new service outage for the property (Manager only).</summary>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Declare(
        [FromBody] DeclareOutageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new DeclareOutageCommand(
                request.Title,
                request.OutageType,
                request.Description,
                request.StartTime,
                request.EndTime),
            cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // ── GET /api/v1/outages ──────────────────────────────────────────────────

    /// <summary>Get all outages for the property (all authenticated roles).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAllOutagesQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/outages/{outageId} ───────────────────────────────────────

    /// <summary>Get a single outage by ID (all authenticated roles).</summary>
    [HttpGet("{outageId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int outageId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOutageByIdQuery(outageId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }
}
