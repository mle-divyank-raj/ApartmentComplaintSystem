using ACLS.Application.Dispatch.Queries.GetDispatchRecommendations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

[Route("api/v1/dispatch")]
public sealed class DispatchController : ApiControllerBase
{
    public DispatchController(IMediator mediator) : base(mediator) { }

    // ── GET /api/v1/dispatch/recommendations/{complaintId} ───────────────────

    /// <summary>Get AI-driven staff assignment recommendations for a complaint (Manager only).</summary>
    [HttpGet("recommendations/{complaintId:int}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecommendations(
        int complaintId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new GetDispatchRecommendationsQuery(complaintId), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(new { complaintId, recommendations = result.Value });
    }
}
