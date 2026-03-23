using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a Complaint is assigned to a StaffMember.
/// Consumed by INotificationService to notify the assigned Staff of the new assignment.
/// </summary>
public sealed record ComplaintAssignedEvent(
    int ComplaintId,
    int AssignedStaffMemberId,
    int PropertyId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
