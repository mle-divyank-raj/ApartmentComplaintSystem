using MediatR;

namespace ACLS.SharedKernel;

/// <summary>
/// Marker interface for all domain events raised by aggregate roots in ACLS.Domain.
/// Extends MediatR.INotification so that domain events can be dispatched via
/// IPublisher.Publish() in Application command handlers and consumed by
/// INotificationHandler&lt;T&gt; implementations in ACLS.Worker.
/// Implement this interface on every domain event record.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>UTC timestamp of when the event occurred.</summary>
    DateTime OccurredAt { get; }
}
