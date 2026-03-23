using ACLS.SharedKernel;

namespace ACLS.Domain.Notifications;

/// <summary>
/// Represents a notification template: a key identifying which template to render
/// and the channel through which it is delivered.
/// Template keys are resolved by the notification infrastructure to provider-specific templates.
/// </summary>
public sealed class NotificationTemplate : ValueObject
{
    /// <summary>
    /// The template key used to look up the correct message template in the notification provider.
    /// Examples: "ComplaintAssigned", "ComplaintResolved", "OutageDeclared", "SosAlert".
    /// </summary>
    public string TemplateKey { get; }

    /// <summary>The delivery channel for this template.</summary>
    public NotificationChannel Channel { get; }

    public NotificationTemplate(string templateKey, NotificationChannel channel)
    {
        Guard.Against.NullOrWhiteSpace(templateKey, nameof(templateKey));
        TemplateKey = templateKey;
        Channel = channel;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return TemplateKey;
        yield return Channel;
    }
}
