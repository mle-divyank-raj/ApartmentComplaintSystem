using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Lightweight Complaint summary DTO for list queries (GetAllComplaints, GetComplaintsByResident).
/// Does not include Media and WorkNotes to reduce payload size.
/// </summary>
public sealed record ComplaintSummaryDto(
    int ComplaintId,
    int UnitId,
    int ResidentId,
    int? AssignedStaffMemberId,
    string Title,
    string Category,
    string Urgency,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt)
{
    public static ComplaintSummaryDto FromDomain(Complaint complaint) => new(
        complaint.ComplaintId,
        complaint.UnitId,
        complaint.ResidentId,
        complaint.AssignedStaffMemberId,
        complaint.Title,
        complaint.Category,
        complaint.Urgency.ToString(),
        complaint.Status.ToString(),
        complaint.CreatedAt,
        complaint.UpdatedAt,
        complaint.ResolvedAt);
}
