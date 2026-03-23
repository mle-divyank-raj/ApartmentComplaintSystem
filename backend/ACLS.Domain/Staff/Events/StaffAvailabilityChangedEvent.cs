using ACLS.SharedKernel;

namespace ACLS.Domain.Staff.Events;

/// <summary>
/// Raised when a StaffMember's Availability state changes.
/// Consumed by audit log handlers and dashboard real-time update handlers.
/// </summary>
public sealed record StaffAvailabilityChangedEvent(
    int StaffMemberId,
    StaffState PreviousState,
    StaffState NewState) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
