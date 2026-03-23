namespace ACLS.SharedKernel;

/// <summary>
/// Marker interface for all domain events raised by aggregate roots in ACLS.Domain.
/// Implement this interface on every domain event class.
/// Domain events are published after state changes are committed and consumed by
/// INotificationHandler implementations in ACLS.Infrastructure and ACLS.Worker.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp of when the event occurred.</summary>
    DateTime OccurredAt { get; }
}
