using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints.Events;
using ACLS.Worker.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.EventHandlers;

/// <summary>
/// Handles FeedbackSubmittedEvent by triggering a recalculation of the assigned
/// StaffMember's AverageRating and writing a FeedbackSubmitted audit entry.
/// </summary>
public sealed class FeedbackSubmittedEventHandler : INotificationHandler<FeedbackSubmittedEvent>
{
    private readonly IAuditRepository _auditRepository;
    private readonly UpdateAverageRatingJob _updateAverageRatingJob;
    private readonly ILogger<FeedbackSubmittedEventHandler> _logger;

    public FeedbackSubmittedEventHandler(
        IAuditRepository auditRepository,
        UpdateAverageRatingJob updateAverageRatingJob,
        ILogger<FeedbackSubmittedEventHandler> logger)
    {
        _auditRepository = auditRepository;
        _updateAverageRatingJob = updateAverageRatingJob;
        _logger = logger;
    }

    public async Task Handle(FeedbackSubmittedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "FeedbackSubmittedEventHandler: ComplaintId {ComplaintId} | StaffMemberId {StaffMemberId} | Rating {ResidentRating} | PropertyId {PropertyId}",
            notification.ComplaintId,
            notification.StaffMemberId,
            notification.ResidentRating,
            notification.PropertyId);

        await _updateAverageRatingJob.ExecuteAsync(
            notification.StaffMemberId,
            notification.PropertyId,
            cancellationToken);

        var audit = AuditEntry.Create(
            action: AuditAction.FeedbackSubmitted,
            entityType: "Complaint",
            entityId: notification.ComplaintId,
            propertyId: notification.PropertyId);

        await _auditRepository.AddAsync(audit, cancellationToken);

        _logger.LogInformation(
            "FeedbackSubmittedEventHandler: AverageRating updated and audit written for StaffMemberId {StaffMemberId}",
            notification.StaffMemberId);
    }
}
