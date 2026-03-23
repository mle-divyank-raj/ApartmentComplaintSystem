using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Full Complaint DTO returned from command handlers (submit, assign, resolve) and GetComplaintById query.
/// Includes all fields including Media and WorkNote collections.
/// </summary>
public sealed record ComplaintDto(
    int ComplaintId,
    int PropertyId,
    int UnitId,
    int ResidentId,
    int? AssignedStaffMemberId,
    string Title,
    string Description,
    string Category,
    List<string> RequiredSkills,
    string Urgency,
    string Status,
    bool PermissionToEnter,
    DateTime? Eta,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt,
    decimal? Tat,
    int? ResidentRating,
    string? ResidentFeedbackComment,
    DateTime? FeedbackSubmittedAt,
    List<MediaDto> Media,
    List<WorkNoteDto> WorkNotes)
{
    public static ComplaintDto FromDomain(Complaint complaint) => new(
        complaint.ComplaintId,
        complaint.PropertyId,
        complaint.UnitId,
        complaint.ResidentId,
        complaint.AssignedStaffMemberId,
        complaint.Title,
        complaint.Description,
        complaint.Category,
        complaint.RequiredSkills,
        complaint.Urgency.ToString(),
        complaint.Status.ToString(),
        complaint.PermissionToEnter,
        complaint.Eta,
        complaint.CreatedAt,
        complaint.UpdatedAt,
        complaint.ResolvedAt,
        complaint.Tat,
        complaint.ResidentRating,
        complaint.ResidentFeedbackComment,
        complaint.FeedbackSubmittedAt,
        complaint.Media.Select(MediaDto.FromDomain).ToList(),
        complaint.WorkNotes.Select(WorkNoteDto.FromDomain).ToList());
}
