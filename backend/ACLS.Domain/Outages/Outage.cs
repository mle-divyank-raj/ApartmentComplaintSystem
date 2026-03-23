using ACLS.SharedKernel;
using ACLS.Domain.Outages.Events;

namespace ACLS.Domain.Outages;

/// <summary>
/// A property-wide service disruption declared by a Manager.
/// Once declared, ACLS.Worker fans out SMS/email notifications to all Residents asynchronously.
/// Outages are immutable after creation, except for MarkNotificationSent and EndTime updates.
/// Table: Outages
/// </summary>
public sealed class Outage : EntityBase
{
    public int OutageId { get; private set; }
    public int PropertyId { get; private set; }
    public int DeclaredByManagerUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public OutageType OutageType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public DateTime DeclaredAt { get; private set; }
    public DateTime? NotificationSentAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Outage() { }

    /// <summary>
    /// Creates and declares a new Outage. Raises OutageDeclaredEvent which triggers the
    /// ACLS.Worker notification fan-out job.
    /// StartTime and EndTime must be UTC.
    /// </summary>
    public static Outage Declare(
        int propertyId,
        int declaredByManagerUserId,
        string title,
        OutageType outageType,
        string description,
        DateTime startTime,
        DateTime? endTime = null)
    {
        Guard.Against.NegativeOrZero(propertyId, nameof(propertyId));
        Guard.Against.NegativeOrZero(declaredByManagerUserId, nameof(declaredByManagerUserId));
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        var outage = new Outage
        {
            PropertyId = propertyId,
            DeclaredByManagerUserId = declaredByManagerUserId,
            Title = title,
            OutageType = outageType,
            Description = description,
            StartTime = startTime,
            EndTime = endTime,
            DeclaredAt = DateTime.UtcNow
        };

        outage.RaiseDomainEvent(
            new OutageDeclaredEvent(outage.OutageId, propertyId, outageType, outage.DeclaredAt));

        return outage;
    }

    /// <summary>
    /// Sets NotificationSentAt = DateTime.UtcNow.
    /// Called by ACLS.Worker after the fan-out batch completes successfully.
    /// </summary>
    public void MarkNotificationSent()
    {
        NotificationSentAt = DateTime.UtcNow;
    }

    /// <summary>Updates the scheduled end time of the outage.</summary>
    public void UpdateEndTime(DateTime? endTime)
    {
        EndTime = endTime;
    }
}
