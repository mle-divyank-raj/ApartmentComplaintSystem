using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Nested summary of the assigned staff member, returned by GetComplaintById.
/// </summary>
public sealed record AssignedStaffMemberDto(
    int StaffMemberId,
    string FullName,
    string? JobTitle,
    string Availability);

/// <summary>
/// Full Complaint DTO returned from command handlers (submit, assign, resolve) and GetComplaintById query.
/// Includes all fields including Media and WorkNote collections.
/// UnitNumber, BuildingName, ResidentName and AssignedStaffMember are populated only by the
/// GetComplaintById read query (not by command handlers that call FromDomain).
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
    // Enriched display fields — populated by the read service, not by FromDomain.
    public string UnitNumber { get; init; } = string.Empty;
    public string BuildingName { get; init; } = string.Empty;
    public string ResidentName { get; init; } = string.Empty;
    public AssignedStaffMemberDto? AssignedStaffMember { get; init; }

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
        complaint.WorkNotes.Select(w => WorkNoteDto.FromDomain(w)).ToList());
}
