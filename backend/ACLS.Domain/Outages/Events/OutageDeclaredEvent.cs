using ACLS.SharedKernel;

namespace ACLS.Domain.Outages.Events;

/// <summary>
/// Raised when a Manager declares a new Outage.
/// Consumed by ACLS.Worker (OutageDeclaredEventHandler) to fan out SMS/email to all Residents.
/// Must meet NFR-12: 500 messages dispatched within 60 seconds.
/// </summary>
public sealed record OutageDeclaredEvent(
    int OutageId,
    int PropertyId,
    OutageType OutageType,
    DateTime DeclaredAt) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
