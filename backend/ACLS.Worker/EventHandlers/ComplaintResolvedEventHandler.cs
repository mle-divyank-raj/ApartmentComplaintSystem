using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints.Events;
using ACLS.Domain.Notifications;
using ACLS.Worker.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.EventHandlers;

/// <summary>
/// Handles ComplaintResolvedEvent:
/// 1. Notifies the Resident that their complaint is resolved and invites feedback.
/// 2. Runs CalculateTatJob to compute and store TAT on the Complaint.
/// 3. Runs UpdateAverageRatingJob to refresh the resolving StaffMember's AverageRating.
/// 4. Writes a ComplaintResolved audit entry.
/// </summary>
public sealed class ComplaintResolvedEventHandler : INotificationHandler<ComplaintResolvedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAuditRepository _auditRepository;
    private readonly CalculateTatJob _calculateTatJob;
    private readonly UpdateAverageRatingJob _updateAverageRatingJob;
    private readonly ILogger<ComplaintResolvedEventHandler> _logger;

    public ComplaintResolvedEventHandler(
        INotificationService notificationService,
        IAuditRepository auditRepository,
        CalculateTatJob calculateTatJob,
        UpdateAverageRatingJob updateAverageRatingJob,
        ILogger<ComplaintResolvedEventHandler> logger)
    {
        _notificationService = notificationService;
        _auditRepository = auditRepository;
        _calculateTatJob = calculateTatJob;
        _updateAverageRatingJob = updateAverageRatingJob;
        _logger = logger;
    }

    public async Task Handle(ComplaintResolvedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ComplaintResolvedEventHandler: ComplaintId {ComplaintId} resolved by StaffMemberId {StaffMemberId} | PropertyId {PropertyId}",
            notification.ComplaintId,
            notification.ResolvedByStaffMemberId,
            notification.PropertyId);

        await _notificationService.NotifyResidentAsync(
            residentId: notification.ResidentId,
            subject: "Your Complaint Has Been Resolved",
            message: $"Complaint #{notification.ComplaintId} has been resolved. " +
                     "Please open the app to submit your feedback and close the ticket.",
            channels: [NotificationChannel.Email, NotificationChannel.InApp],
            ct: cancellationToken);

        await _calculateTatJob.ExecuteAsync(
            notification.ComplaintId,
            notification.PropertyId,
            cancellationToken);

        await _updateAverageRatingJob.ExecuteAsync(
            notification.ResolvedByStaffMemberId,
            notification.PropertyId,
            cancellationToken);

        var audit = AuditEntry.Create(
            action: AuditAction.ComplaintResolved,
            entityType: "Complaint",
            entityId: notification.ComplaintId,
            propertyId: notification.PropertyId);

        await _auditRepository.AddAsync(audit, cancellationToken);

        _logger.LogInformation(
            "ComplaintResolvedEventHandler: TAT calculated, rating updated, and audit written for ComplaintId {ComplaintId}",
            notification.ComplaintId);
    }
}
