using ACLS.Domain.Outages.Events;
using ACLS.Worker.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.EventHandlers;

/// <summary>
/// Handles OutageDeclaredEvent by dispatching BroadcastOutageNotificationJob to fan out
/// SMS/email notifications to all Residents of the affected Property.
/// The broadcast meets NFR-12 (500 messages within 60 seconds) via concurrent Task.WhenAll
/// in BroadcastOutageNotificationJob → NotificationService.BroadcastOutageAsync.
/// </summary>
public sealed class OutageDeclaredEventHandler : INotificationHandler<OutageDeclaredEvent>
{
    private readonly BroadcastOutageNotificationJob _broadcastJob;
    private readonly ILogger<OutageDeclaredEventHandler> _logger;

    public OutageDeclaredEventHandler(
        BroadcastOutageNotificationJob broadcastJob,
        ILogger<OutageDeclaredEventHandler> logger)
    {
        _broadcastJob = broadcastJob;
        _logger = logger;
    }

    public async Task Handle(OutageDeclaredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OutageDeclaredEventHandler: OutageId {OutageId} declared | PropertyId {PropertyId} | Type: {OutageType}",
            notification.OutageId,
            notification.PropertyId,
            notification.OutageType);

        await _broadcastJob.ExecuteAsync(
            notification.OutageId,
            notification.PropertyId,
            cancellationToken);
    }
}
