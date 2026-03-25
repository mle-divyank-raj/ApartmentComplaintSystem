using ACLS.Domain.AuditLog;
using ACLS.Domain.Notifications;
using ACLS.Domain.Outages;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.Jobs;

/// <summary>
/// Sends outage notifications to all Residents of the affected Property.
/// Delegates concurrent fan-out to INotificationService.BroadcastOutageAsync which uses
/// Task.WhenAll internally — meeting NFR-12 (500 messages within 60 seconds).
/// After broadcast completes: marks Outage.NotificationSentAt and writes an audit entry.
/// Idempotent: if NotificationSentAt is already set the job exits without resending.
/// </summary>
public sealed class BroadcastOutageNotificationJob
{
    private readonly IOutageRepository _outageRepository;
    private readonly INotificationService _notificationService;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<BroadcastOutageNotificationJob> _logger;

    public BroadcastOutageNotificationJob(
        IOutageRepository outageRepository,
        INotificationService notificationService,
        IAuditRepository auditRepository,
        ILogger<BroadcastOutageNotificationJob> logger)
    {
        _outageRepository = outageRepository;
        _notificationService = notificationService;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(int outageId, int propertyId, CancellationToken ct)
    {
        _logger.LogInformation(
            "BroadcastOutageNotificationJob: Starting for OutageId {OutageId} | PropertyId {PropertyId}",
            outageId, propertyId);

        var outage = await _outageRepository.GetByIdAsync(outageId, propertyId, ct);

        if (outage is null)
        {
            _logger.LogWarning(
                "BroadcastOutageNotificationJob: OutageId {OutageId} not found for PropertyId {PropertyId}; broadcast skipped",
                outageId, propertyId);
            return;
        }

        if (outage.NotificationSentAt.HasValue)
        {
            _logger.LogInformation(
                "BroadcastOutageNotificationJob: OutageId {OutageId} already notified at {SentAt}; skipping duplicate broadcast",
                outageId, outage.NotificationSentAt.Value);
            return;
        }

        // Concurrent fan-out delegated to NotificationService (Task.WhenAll — NFR-12).
        await _notificationService.BroadcastOutageAsync(outage, ct);

        outage.MarkNotificationSent();
        await _outageRepository.UpdateAsync(outage, ct);

        var audit = AuditEntry.Create(
            action: AuditAction.OutageDeclared,
            entityType: "Outage",
            entityId: outageId,
            propertyId: propertyId);

        await _auditRepository.AddAsync(audit, ct);

        _logger.LogInformation(
            "BroadcastOutageNotificationJob: Broadcast complete | OutageId {OutageId} | NotificationSentAt: {SentAt}",
            outageId, outage.NotificationSentAt!.Value);
    }
}
