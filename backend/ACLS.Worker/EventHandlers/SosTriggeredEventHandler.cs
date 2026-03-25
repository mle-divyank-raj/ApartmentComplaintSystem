using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints;
using ACLS.Domain.Complaints.Events;
using ACLS.Domain.Notifications;
using ACLS.Domain.Staff;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.EventHandlers;

/// <summary>
/// Handles SosTriggeredEvent by concurrently notifying all available StaffMembers
/// of the SOS_EMERGENCY. Delegates fan-out to INotificationService.NotifyAllOnCallStaffAsync
/// which uses Task.WhenAll to meet NFR-12.
///
/// Domain repository interfaces are injected directly because
/// INotificationService.NotifyAllOnCallStaffAsync requires full StaffMember domain entities
/// (not DTOs), which cannot be obtained via Application layer queries.
/// </summary>
public sealed class SosTriggeredEventHandler : INotificationHandler<SosTriggeredEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IStaffRepository _staffRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<SosTriggeredEventHandler> _logger;

    public SosTriggeredEventHandler(
        INotificationService notificationService,
        IStaffRepository staffRepository,
        IComplaintRepository complaintRepository,
        IAuditRepository auditRepository,
        ILogger<SosTriggeredEventHandler> logger)
    {
        _notificationService = notificationService;
        _staffRepository = staffRepository;
        _complaintRepository = complaintRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task Handle(SosTriggeredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SosTriggeredEventHandler: ComplaintId {ComplaintId} | PropertyId {PropertyId}",
            notification.ComplaintId,
            notification.PropertyId);

        var complaint = await _complaintRepository.GetByIdAsync(
            notification.ComplaintId,
            notification.PropertyId,
            cancellationToken);

        if (complaint is null)
        {
            _logger.LogWarning(
                "SosTriggeredEventHandler: ComplaintId {ComplaintId} not found for PropertyId {PropertyId}; SOS blast skipped",
                notification.ComplaintId,
                notification.PropertyId);
        }
        else
        {
            var availableStaff = await _staffRepository.GetAvailableAsync(
                notification.PropertyId,
                cancellationToken);

            if (availableStaff.Count == 0)
            {
                _logger.LogWarning(
                    "SosTriggeredEventHandler: No available staff for PropertyId {PropertyId} during SOS on ComplaintId {ComplaintId}",
                    notification.PropertyId,
                    notification.ComplaintId);
            }
            else
            {
                // NotifyAllOnCallStaffAsync fans out via Task.WhenAll — NFR-12 compliant.
                await _notificationService.NotifyAllOnCallStaffAsync(
                    availableStaff,
                    complaint,
                    cancellationToken);

                _logger.LogInformation(
                    "SosTriggeredEventHandler: SOS blast sent to {StaffCount} staff members for ComplaintId {ComplaintId}",
                    availableStaff.Count,
                    notification.ComplaintId);
            }
        }

        var audit = AuditEntry.Create(
            action: AuditAction.SosTriggered,
            entityType: "Complaint",
            entityId: notification.ComplaintId,
            propertyId: notification.PropertyId);

        await _auditRepository.AddAsync(audit, cancellationToken);
    }
}
