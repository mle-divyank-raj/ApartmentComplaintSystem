using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Lightweight Complaint summary DTO for list queries (GetAllComplaints, GetComplaintsByResident).
/// Does not include Media and WorkNotes to reduce payload size.
/// ResidentName, UnitNumber, BuildingName and AssignedStaffMemberName are resolved via joined
/// projection — see IComplaintReadService.
/// </summary>
public sealed record ComplaintSummaryDto(
    int ComplaintId,
    int UnitId,
    string UnitNumber,
    string BuildingName,
    int ResidentId,
    string ResidentName,
    int? AssignedStaffMemberId,
    string? AssignedStaffMemberName,
    string Title,
    string Category,
    string Urgency,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ResolvedAt);
