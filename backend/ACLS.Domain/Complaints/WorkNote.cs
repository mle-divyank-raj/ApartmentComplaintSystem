using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints;

/// <summary>
/// A freetext note added by a StaffMember to an active Complaint.
/// Records what the staff member observed or did during the job.
/// Multiple WorkNotes may be added to a single Complaint.
/// Table: WorkNotes
/// </summary>
public sealed class WorkNote : EntityBase
{
    public int WorkNoteId { get; private set; }
    public int ComplaintId { get; private set; }
    public int StaffMemberId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private WorkNote() { }

    /// <summary>
    /// Creates a new WorkNote on a Complaint. Content must not be blank.
    /// Max length is governed by ComplaintConstants (enforced at application layer via FluentValidation).
    /// </summary>
    public static WorkNote Create(int complaintId, int staffMemberId, string content)
    {
        Guard.Against.NegativeOrZero(complaintId, nameof(complaintId));
        Guard.Against.NegativeOrZero(staffMemberId, nameof(staffMemberId));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));

        return new WorkNote
        {
            ComplaintId = complaintId,
            StaffMemberId = staffMemberId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }
}
