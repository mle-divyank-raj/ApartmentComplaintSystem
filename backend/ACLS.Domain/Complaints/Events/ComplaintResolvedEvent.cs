using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a Complaint transitions to RESOLVED status.
/// Consumed by INotificationService (notify resident), ACLS.Worker (TAT calculation, rating recalculation).
/// </summary>
public sealed record ComplaintResolvedEvent(
    int ComplaintId,
    int ResolvedByStaffMemberId,
    int PropertyId,
    int ResidentId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
