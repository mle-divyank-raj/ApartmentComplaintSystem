using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a Complaint transitions between any two TicketStatus values.
/// Consumed by audit log handlers and real-time dashboard subscribers.
/// </summary>
public sealed record ComplaintStatusChangedEvent(
    int ComplaintId,
    TicketStatus PreviousStatus,
    TicketStatus NewStatus,
    int PropertyId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
