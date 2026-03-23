namespace ACLS.Domain.Notifications;

/// <summary>
/// The delivery mechanism for a Notification.
/// Stored as nvarchar(50) strings in any notification configuration tables.
/// The specific provider (Twilio, SendGrid, etc.) is an infrastructure concern.
/// </summary>
public enum NotificationChannel
{
    Email,
    SMS,
    InApp,
    Push
}
