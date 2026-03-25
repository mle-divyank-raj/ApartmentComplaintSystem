using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints.Events;

/// <summary>
/// Raised when a Resident submits feedback and closes a Complaint (RESOLVED → CLOSED).
/// Consumed by ACLS.Worker (FeedbackSubmittedEventHandler) to trigger recalculation
/// of the assigned StaffMember's AverageRating.
/// </summary>
public sealed record FeedbackSubmittedEvent(
    int ComplaintId,
    int PropertyId,
    int StaffMemberId,
    int ResidentRating) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
