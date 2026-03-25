using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints.Events;
using ACLS.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.EventHandlers;

/// <summary>
/// Handles ComplaintAssignedEvent by notifying the assigned StaffMember of their new task
/// and writing a ComplaintAssigned audit entry.
/// </summary>
public sealed class ComplaintAssignedEventHandler : INotificationHandler<ComplaintAssignedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<ComplaintAssignedEventHandler> _logger;

    public ComplaintAssignedEventHandler(
        INotificationService notificationService,
        IAuditRepository auditRepository,
        ILogger<ComplaintAssignedEventHandler> logger)
    {
        _notificationService = notificationService;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(ComplaintAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ComplaintAssignedEventHandler: ComplaintId {ComplaintId} assigned to StaffMemberId {StaffMemberId} | PropertyId {PropertyId}",
            notification.ComplaintId,
            notification.AssignedStaffMemberId,
            notification.PropertyId);

        await _notificationService.NotifyStaffAsync(
            staffMemberId: notification.AssignedStaffMemberId,
            subject: "New Complaint Assignment",
            message: $"Complaint #{notification.ComplaintId} has been assigned to you. " +
                     "Please open the StaffApp to review and accept the assignment.",
            channels: [NotificationChannel.Email, NotificationChannel.SMS],
            ct: cancellationToken);

        var audit = AuditEntry.Create(
            action: AuditAction.ComplaintAssigned,
            entityType: "Complaint",
            entityId: notification.ComplaintId,
            propertyId: notification.PropertyId);

        await _auditRepository.AddAsync(audit, cancellationToken);

        _logger.LogInformation(
            "ComplaintAssignedEventHandler: Notification sent and audit written for ComplaintId {ComplaintId}",
            notification.ComplaintId);
    }
}
