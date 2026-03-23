using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a new Complaint is created by a Resident.
/// Consumed by INotificationService to notify the Manager of a new submitted complaint.
/// </summary>
public sealed record ComplaintSubmittedEvent(
    int ComplaintId,
    int PropertyId,
    int ResidentId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
