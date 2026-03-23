using ACLS.Domain.Complaints;
using ACLS.Domain.Outages;
using ACLS.Domain.Staff;

namespace ACLS.Domain.Notifications;

/// <summary>
/// Notification service interface. Sends automated messages to Residents and StaffMembers
/// triggered by domain events. All provider details (Twilio, SendGrid, FCM, APNs) are
/// infrastructure concerns hidden behind this interface.
/// Defined in Domain; implemented in ACLS.Infrastructure.Notifications.NotificationService.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a Resident about a change to their Complaint.
    /// Called after ComplaintResolvedEvent, ETA updates, and status changes.
    /// </summary>
    Task NotifyResidentAsync(
        int residentId,
        string subject,
        string message,
        IEnumerable<NotificationChannel> channels,
        CancellationToken ct);

    /// <summary>
    /// Sends a notification to a StaffMember about a new or updated assignment.
    /// Called after ComplaintAssignedEvent.
    /// </summary>
    Task NotifyStaffAsync(
        int staffMemberId,
        string subject,
        string message,
        IEnumerable<NotificationChannel> channels,
        CancellationToken ct);

    /// <summary>
    /// Broadcasts an Outage notification to ALL Residents of the affected Property.
    /// Fan-out is handled asynchronously by ACLS.Worker to meet NFR-12 (500 messages / 60 sec).
    /// </summary>
    Task BroadcastOutageAsync(Outage outage, CancellationToken ct);

    /// <summary>
    /// Simultaneously notifies all on-call StaffMembers of an SOS_EMERGENCY complaint.
    /// Called by TriggerSosCommandHandler. Must dispatch to all provided staff concurrently.
    /// </summary>
    Task NotifyAllOnCallStaffAsync(
        IEnumerable<StaffMember> onCallStaff,
        Complaint complaint,
        CancellationToken ct);
}
