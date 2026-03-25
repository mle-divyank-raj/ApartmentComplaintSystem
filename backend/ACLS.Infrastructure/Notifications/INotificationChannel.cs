using ACLS.Domain.Notifications;

namespace ACLS.Infrastructure.Notifications;

/// <summary>
/// Infrastructure abstraction for a single notification delivery channel (Email or SMS).
/// Implementations read provider credentials from environment variables as specified in
/// environment_config.md Section 3.4. The correct implementation is selected at DI registration time.
/// Multiple implementations are registered and collected via IEnumerable&lt;INotificationChannel&gt;
/// by NotificationService.
/// </summary>
public interface INotificationChannel
{
    /// <summary>The delivery channel type this provider handles.</summary>
    NotificationChannel ChannelType { get; }

    /// <summary>
    /// Sends a single message to the specified recipient address.
    /// For Email: recipient is an email address.
    /// For SMS: recipient is an E.164 phone number.
    /// </summary>
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct);
}
