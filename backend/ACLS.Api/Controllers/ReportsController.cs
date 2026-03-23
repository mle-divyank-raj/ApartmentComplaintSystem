using ACLS.Application.Reports.Queries.GetComplaintsByUnit;
using ACLS.Application.Reports.Queries.GetComplaintsSummary;
using ACLS.Application.Reports.Queries.GetDashboardMetrics;
using ACLS.Application.Reports.Queries.GetStaffPerformance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ACLS.Api.Controllers;

[Route("api/v1/reports")]
[Authorize(Roles = "Manager")]
public sealed class ReportsController : ApiControllerBase
{
    public ReportsController(IMediator mediator) : base(mediator) { }

    // ── GET /api/v1/reports/dashboard ────────────────────────────────────────

    /// <summary>Get high-level dashboard metrics for the property (Manager only).</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDashboardMetricsQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/reports/staff-performance ────────────────────────────────

    /// <summary>Get performance metrics for all staff members (Manager only).</summary>
    [HttpGet("staff-performance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaffPerformance(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStaffPerformanceQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/reports/unit-history/{unitId} ────────────────────────────

    /// <summary>Get the full complaint history for a specific unit (Manager only).</summary>
    [HttpGet("unit-history/{unitId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitHistory(
        int unitId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetComplaintsByUnitQuery(unitId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/reports/complaints-summary ───────────────────────────────

    /// <summary>Get a filterable summary of complaints (Manager only).</summary>
    [HttpGet("complaints-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplaintsSummary(CancellationToken cancellationToken)
    {
        // ComplaintQueryOptions can be extended here as query params in a future iteration.
        var result = await Mediator.Send(new GetComplaintsSummaryQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }
}
