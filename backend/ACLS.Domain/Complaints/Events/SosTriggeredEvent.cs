using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a Resident triggers the SOS protocol on a Complaint.
/// Forces Urgency = SOS_EMERGENCY and Status = ASSIGNED.
/// Consumed by ACLS.Worker (SosTriggeredEventHandler) to concurrently notify all
/// available StaffMembers of the emergency in the affected Property.
/// </summary>
public sealed record SosTriggeredEvent(
    int ComplaintId,
    int PropertyId,
    int ResidentId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
