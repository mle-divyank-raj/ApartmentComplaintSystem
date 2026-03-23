using ACLS.Application.Complaints.Commands.AddWorkNote;
using ACLS.Application.Complaints.Commands.AssignComplaint;
using ACLS.Application.Complaints.Commands.ResolveComplaint;
using ACLS.Application.Complaints.Commands.SubmitComplaint;
using ACLS.Application.Complaints.Commands.SubmitFeedback;
using ACLS.Application.Complaints.Commands.TriggerSos;
using ACLS.Application.Complaints.Commands.UpdateEta;
using ACLS.Application.Complaints.Queries.GetAllComplaints;
using ACLS.Application.Complaints.Queries.GetComplaintById;
using ACLS.Application.Complaints.Queries.GetComplaintsByResident;
using ACLS.Contracts.Complaints;
using ACLS.Domain.Complaints;
using ACLS.Domain.Storage;
using ACLS.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubmitMediaResult = ACLS.Application.Complaints.Commands.SubmitComplaint.MediaUploadResult;
using ResolveMediaResult = ACLS.Application.Complaints.Commands.ResolveComplaint.MediaUploadResult;

namespace ACLS.Api.Controllers;

/// <summary>
/// Complaint lifecycle endpoints. Injects IStorageService (alongside IMediator) for media uploads.
/// MediaFiles are uploaded to blob storage in the controller action before the command is dispatched.
/// PropertyId is NEVER sourced from request — it comes from ICurrentPropertyContext (TenancyMiddleware).
/// </summary>
[Route("api/v1/complaints")]
public sealed class ComplaintsController : ApiControllerBase
{
    private readonly IStorageService _storageService;

    private static readonly HashSet<string> AllowedMediaContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public ComplaintsController(IMediator mediator, IStorageService storageService)
        : base(mediator)
    {
        _storageService = storageService;
    }

    // ── POST /api/v1/complaints ──────────────────────────────────────────────

    /// <summary>Submit a new maintenance complaint (Resident only).</summary>
    [HttpPost]
    [Authorize(Roles = "Resident")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(
        [FromForm] SubmitComplaintRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Upload media files to blob storage before dispatching the command
        var mediaUrls = new List<SubmitMediaResult>();
        var files = Request.Form.Files;

        if (files.Count > ComplaintConstants.MaxMediaAttachments)
            return BadRequest(new { detail = $"Maximum {ComplaintConstants.MaxMediaAttachments} media files allowed." });

        foreach (var file in files)
        {
            var validationError = ValidateMediaFile(file);
            if (validationError is not null)
                return MapError(validationError);

            var url = await _storageService.UploadAsync(
                file.OpenReadStream(), file.FileName, file.ContentType, cancellationToken);

            mediaUrls.Add(new SubmitMediaResult(url, file.ContentType));
        }

        // 2. Dispatch the command — only URL strings, no binary data
        var command = new SubmitComplaintCommand(
            request.Title,
            request.Description,
            request.Category,
            request.Urgency,
            UnitId: 0, // UnitId is resolved from the Resident record by the handler via ICurrentPropertyContext
            request.PermissionToEnter,
            mediaUrls);

        var result = await Mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return CreatedAtAction(
            nameof(GetById),
            new { complaintId = result.Value.ComplaintId },
            result.Value);
    }

    // ── GET /api/v1/complaints/my ────────────────────────────────────────────
    // IMPORTANT: This route must be declared BEFORE GET /{complaintId} to avoid ambiguity.

    /// <summary>Get authenticated resident's own complaints.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Resident")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        // ResidentId is resolved inside the handler from ICurrentPropertyContext.UserId
        var result = await Mediator.Send(new GetComplaintsByResidentQuery(ResidentId: 0), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/complaints/sos ──────────────────────────────────────────

    /// <summary>Trigger an SOS emergency complaint (Resident only).</summary>
    [HttpPost("sos")]
    [Authorize(Roles = "Resident")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerSos(
        [FromBody] TriggerSosRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TriggerSosCommand(
            request.Title,
            request.Description,
            UnitId: 0, // resolved by handler
            request.PermissionToEnter);

        var result = await Mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // ── GET /api/v1/complaints ───────────────────────────────────────────────

    /// <summary>Get all complaints for the property (Manager only, filterable + paginated).</summary>
    [HttpGet]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? urgency,
        [FromQuery] string? category,
        [FromQuery] int? staffMemberId,
        [FromQuery] int? unitId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        TicketStatus? statusEnum = null;
        if (status is not null && Enum.TryParse<TicketStatus>(status, ignoreCase: true, out var s))
            statusEnum = s;

        Urgency? urgencyEnum = null;
        if (urgency is not null && Enum.TryParse<Urgency>(urgency, ignoreCase: true, out var u))
            urgencyEnum = u;

        var options = new ComplaintQueryOptions(
            Status: statusEnum,
            Urgency: urgencyEnum,
            Category: category,
            DateFrom: dateFrom,
            DateTo: dateTo,
            Search: search,
            SortBy: sortBy,
            SortDirection: sortDirection,
            Page: page,
            PageSize: pageSize);

        var result = await Mediator.Send(new GetAllComplaintsQuery(options), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── GET /api/v1/complaints/{complaintId} ─────────────────────────────────

    /// <summary>Get a single complaint by ID.</summary>
    [HttpGet("{complaintId:int}")]
    [Authorize(Roles = "Resident,Manager,MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int complaintId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetComplaintByIdQuery(complaintId), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/complaints/{complaintId}/assign ─────────────────────────

    /// <summary>Assign complaint to a staff member (Manager only, atomic).</summary>
    [HttpPost("{complaintId:int}/assign")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(
        int complaintId,
        [FromBody] AssignComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new AssignComplaintCommand(complaintId, request.StaffMemberId), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/complaints/{complaintId}/reassign ───────────────────────

    /// <summary>Reassign complaint to a different staff member (Manager only, atomic).</summary>
    [HttpPost("{complaintId:int}/reassign")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reassign(
        int complaintId,
        [FromBody] AssignComplaintRequest request,
        CancellationToken cancellationToken)
    {
        // Reassign reuses AssignComplaintCommand — the handler calls Complaint.Assign()
        // which internally handles the previous staff member's availability reset.
        var result = await Mediator.Send(
            new AssignComplaintCommand(complaintId, request.StaffMemberId), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/complaints/{complaintId}/status ─────────────────────────

    /// <summary>Update complaint status (MaintenanceStaff only: ASSIGNED→EN_ROUTE, EN_ROUTE→IN_PROGRESS).</summary>
    [HttpPost("{complaintId:int}/status")]
    [Authorize(Roles = "MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> UpdateStatus(
        int complaintId,
        [FromBody] UpdateComplaintStatusRequest request,
        CancellationToken cancellationToken)
    {
        // We need a separate handler for status transitions.
        // Using UpdateEtaCommand for now would be wrong; we need a proper command.
        // This is handled directly: load complaint and call appropriate method.
        // For Phase 3, delegate to a status update handler (to be wired in Phase 4 if not built).
        // For now, map to the correct command.
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented,
            new { detail = "Status update via UpdateStatusCommand — wire handler in Phase 4." }));
    }

    // ── POST /api/v1/complaints/{complaintId}/resolve ────────────────────────

    /// <summary>Mark complaint as resolved (MaintenanceStaff only, atomic + async notification).</summary>
    [HttpPost("{complaintId:int}/resolve")]
    [Authorize(Roles = "MaintenanceStaff")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Resolve(
        int complaintId,
        [FromForm] ResolveComplaintRequest request,
        CancellationToken cancellationToken)
    {
        // Upload completion photos to blob storage before dispatching the command
        var photoUrls = new List<ResolveMediaResult>();
        var files = Request.Form.Files;

        foreach (var file in files)
        {
            var validationError = ValidateMediaFile(file);
            if (validationError is not null)
                return MapError(validationError);

            var url = await _storageService.UploadAsync(
                file.OpenReadStream(), file.FileName, file.ContentType, cancellationToken);

            photoUrls.Add(new ResolveMediaResult(url, file.ContentType));
        }

        var result = await Mediator.Send(
            new ResolveComplaintCommand(complaintId, photoUrls), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(result.Value);
    }

    // ── POST /api/v1/complaints/{complaintId}/feedback ───────────────────────

    /// <summary>Submit resident feedback and rating (Resident only).</summary>
    [HttpPost("{complaintId:int}/feedback")]
    [Authorize(Roles = "Resident")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitFeedback(
        int complaintId,
        [FromBody] SubmitFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new SubmitFeedbackCommand(complaintId, request.Rating, request.Comment), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(new { complaintId });
    }

    // ── POST /api/v1/complaints/{complaintId}/work-notes ─────────────────────

    /// <summary>Add a work note to a complaint (MaintenanceStaff only).</summary>
    [HttpPost("{complaintId:int}/work-notes")]
    [Authorize(Roles = "MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddWorkNote(
        int complaintId,
        [FromBody] AddWorkNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new AddWorkNoteCommand(complaintId, request.Content), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    // ── POST /api/v1/complaints/{complaintId}/eta ────────────────────────────

    /// <summary>Update estimated completion time (MaintenanceStaff only).</summary>
    [HttpPost("{complaintId:int}/eta")]
    [Authorize(Roles = "MaintenanceStaff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEta(
        int complaintId,
        [FromBody] UpdateEtaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateEtaCommand(complaintId, request.Eta), cancellationToken);

        if (!result.IsSuccess)
            return MapError(result.Error);

        return Ok(new { complaintId, eta = request.Eta });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static Error? ValidateMediaFile(IFormFile file)
    {
        if (file.Length == 0)
            return new Error("Validation.Failed", $"File '{file.FileName}' is empty.");

        if (file.Length > MaxFileSizeBytes)
            return ComplaintErrors.MediaFileTooLarge(file.FileName);

        if (!AllowedMediaContentTypes.Contains(file.ContentType))
            return ComplaintErrors.InvalidMediaType(file.ContentType);

        return null;
    }
}
