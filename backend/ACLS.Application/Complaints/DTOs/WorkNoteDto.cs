using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Data Transfer Object for a WorkNote added by a StaffMember to a Complaint.
/// </summary>
public sealed record WorkNoteDto(
    int WorkNoteId,
    int StaffMemberId,
    string StaffMemberName,
    string Content,
    DateTime CreatedAt)
{
    public static WorkNoteDto FromDomain(WorkNote workNote, string staffMemberName = "") => new(
        workNote.WorkNoteId,
        workNote.StaffMemberId,
        staffMemberName,
        workNote.Content,
        workNote.CreatedAt);
}
