using ACLS.SharedKernel;
using ACLS.Domain.Complaints.Events;

namespace ACLS.Domain.Complaints;

/// <summary>
/// Central aggregate root of the ACLS system. Represents a maintenance request raised by a Resident.
/// All state transitions are enforced through domain methods that validate invariants and raise
/// domain events. Properties are private set — no external code may mutate state directly.
/// Table: Complaints
/// </summary>
public sealed class Complaint : EntityBase
{
    public int ComplaintId { get; private set; }
    public int PropertyId { get; private set; }
    public int UnitId { get; private set; }
    public int ResidentId { get; private set; }
    public int? AssignedStaffMemberId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public List<string> RequiredSkills { get; private set; } = [];
    public Urgency Urgency { get; private set; }
    public TicketStatus Status { get; private set; }
    public bool PermissionToEnter { get; private set; }
    public DateTime? Eta { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public decimal? Tat { get; private set; }
    public int? ResidentRating { get; private set; }
    public string? ResidentFeedbackComment { get; private set; }
    public DateTime? FeedbackSubmittedAt { get; private set; }

    private readonly List<Media> _media = [];
    /// <summary>Media attachments linked to this Complaint.</summary>
    public IReadOnlyList<Media> Media => _media.AsReadOnly();

    private readonly List<WorkNote> _workNotes = [];
    /// <summary>Work notes added by the assigned StaffMember.</summary>
    public IReadOnlyList<WorkNote> WorkNotes => _workNotes.AsReadOnly();

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Complaint() { }

    /// <summary>
    /// Creates a new Complaint in OPEN status. RequiredSkills are derived from Category
    /// at the application layer and passed in. Category examples: "Plumbing", "Electrical", "HVAC".
    /// </summary>
    public static Complaint Create(
        string title,
        string description,
        string category,
        Urgency urgency,
        int unitId,
        int residentId,
        int propertyId,
        bool permissionToEnter,
        List<string>? requiredSkills = null)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(category, nameof(category));
        Guard.Against.NegativeOrZero(unitId, nameof(unitId));
        Guard.Against.NegativeOrZero(residentId, nameof(residentId));
        Guard.Against.NegativeOrZero(propertyId, nameof(propertyId));

        var complaint = new Complaint
        {
            Title = title,
            Description = description,
            Category = category,
            Urgency = urgency,
            Status = TicketStatus.OPEN,
            UnitId = unitId,
            ResidentId = residentId,
            PropertyId = propertyId,
            PermissionToEnter = permissionToEnter,
            RequiredSkills = requiredSkills ?? [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        complaint.RaiseDomainEvent(
            new ComplaintSubmittedEvent(complaint.ComplaintId, propertyId, residentId));

        return complaint;
    }

    /// <summary>
    /// Assigns the Complaint to a StaffMember. Valid from OPEN or ASSIGNED (reassignment).
    /// Caller must also call StaffMember.MarkBusy() in the same transaction.
    /// </summary>
    public Result Assign(int staffMemberId)
    {
        if (Status != TicketStatus.OPEN && Status != TicketStatus.ASSIGNED)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.ASSIGNED));

        Guard.Against.NegativeOrZero(staffMemberId, nameof(staffMemberId));

        Status = TicketStatus.ASSIGNED;
        AssignedStaffMemberId = staffMemberId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(
            new ComplaintAssignedEvent(ComplaintId, staffMemberId, PropertyId));

        return Result.Success();
    }

    /// <summary>
    /// Triggers SOS protocol — forces Urgency to SOS_EMERGENCY and Status to ASSIGNED.
    /// Called by TriggerSosCommandHandler which then broadcasts to all on-call staff.
    /// </summary>
    public Result TriggerSos()
    {
        if (Status == TicketStatus.RESOLVED || Status == TicketStatus.CLOSED)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.ASSIGNED));

        var previousStatus = Status;
        Urgency = Urgency.SOS_EMERGENCY;
        Status = TicketStatus.ASSIGNED;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(
            new ComplaintStatusChangedEvent(ComplaintId, previousStatus, TicketStatus.ASSIGNED, PropertyId));

        return Result.Success();
    }

    /// <summary>
    /// Staff accepts the assignment — transitions ASSIGNED → EN_ROUTE.
    /// Called when Staff acknowledges their assignment via the StaffApp.
    /// </summary>
    public Result AcceptAssignment()
    {
        if (Status != TicketStatus.ASSIGNED)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.EN_ROUTE));

        var previousStatus = Status;
        Status = TicketStatus.EN_ROUTE;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(
            new ComplaintStatusChangedEvent(ComplaintId, previousStatus, Status, PropertyId));

        return Result.Success();
    }

    /// <summary>
    /// Staff begins work at the unit — transitions EN_ROUTE → IN_PROGRESS.
    /// </summary>
    public Result StartWork()
    {
        if (Status != TicketStatus.EN_ROUTE)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.IN_PROGRESS));

        var previousStatus = Status;
        Status = TicketStatus.IN_PROGRESS;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(
            new ComplaintStatusChangedEvent(ComplaintId, previousStatus, Status, PropertyId));

        return Result.Success();
    }

    /// <summary>
    /// Staff marks the Complaint resolved — transitions IN_PROGRESS → RESOLVED.
    /// Sets ResolvedAt = DateTime.UtcNow. TAT is calculated asynchronously by ACLS.Worker.
    /// Caller must also call StaffMember.MarkAvailable() in the same transaction.
    /// </summary>
    public Result Resolve()
    {
        if (Status != TicketStatus.IN_PROGRESS)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.RESOLVED));

        if (AssignedStaffMemberId is null)
            throw new InvalidOperationException(
                "Cannot resolve a Complaint that has no assigned StaffMember.");

        var previousStatus = Status;
        Status = TicketStatus.RESOLVED;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(
            new ComplaintResolvedEvent(ComplaintId, AssignedStaffMemberId.Value, PropertyId, ResidentId));

        RaiseDomainEvent(
            new ComplaintStatusChangedEvent(ComplaintId, previousStatus, Status, PropertyId));

        return Result.Success();
    }

    /// <summary>
    /// Resident submits feedback — transitions RESOLVED → CLOSED.
    /// Rating must be between 1 and 5 (inclusive).
    /// </summary>
    public Result Close(int residentRating, string? feedbackComment)
    {
        if (Status != TicketStatus.RESOLVED)
            return Result.Failure(
                ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.CLOSED));

        if (residentRating < ComplaintConstants.MinRating || residentRating > ComplaintConstants.MaxRating)
            return Result.Failure(ComplaintErrors.InvalidRating(residentRating));

        Status = TicketStatus.CLOSED;
        ResidentRating = residentRating;
        ResidentFeedbackComment = feedbackComment;
        FeedbackSubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the ETA set by the assigned StaffMember. ETA must be a future UTC timestamp.
    /// Resident is notified of ETA changes by the application layer event handler.
    /// </summary>
    public Result UpdateEta(DateTime eta)
    {
        if (Status == TicketStatus.RESOLVED || Status == TicketStatus.CLOSED)
            return Result.Failure(ComplaintErrors.CannotUpdateEtaOnClosedComplaint());

        if (eta <= DateTime.UtcNow)
            return Result.Failure(ComplaintErrors.EtaMustBeInFuture());

        Eta = eta;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Records the computed TAT (minutes from CreatedAt to ResolvedAt).
    /// Called by ACLS.Worker after ComplaintResolvedEvent. Never called inline.
    /// </summary>
    public void RecordTat(decimal tatMinutes)
    {
        Guard.Against.Negative((int)tatMinutes, nameof(tatMinutes));
        Tat = tatMinutes;
    }

    /// <summary>
    /// Adds a Media attachment to this Complaint's in-memory collection.
    /// The Media entity must have been persisted separately via IMediaRepository or the DbContext
    /// before this navigation property is populated.
    /// This method is used to maintain the in-memory list for domain invariant checks.
    /// </summary>
    public Result AddMedia(Media media)
    {
        Guard.Against.Null(media, nameof(media));

        var residentUploadedCount = _media.Count(m => m.UploadedByUserId == media.UploadedByUserId);
        if (residentUploadedCount >= ComplaintConstants.MaxMediaAttachments)
            return Result.Failure(ComplaintErrors.MaxMediaAttachmentsExceeded());

        _media.Add(media);
        return Result.Success();
    }
}
