namespace ACLS.Application.Reports.DTOs;

/// <summary>
/// Real-time dashboard metrics returned for the Manager dashboard view.
/// </summary>
public sealed record DashboardMetricsDto(
    int OpenCount,
    int AssignedCount,
    int InProgressCount,
    int ResolvedCount,
    int ClosedCount,
    int SosActiveCount,
    List<ActiveAssignmentDto> ActiveAssignments,
    List<StaffAvailabilitySummaryDto> StaffAvailabilitySummary);

/// <summary>Summary of a single active complaint assignment for the dashboard.</summary>
public sealed record ActiveAssignmentDto(
    int ComplaintId,
    string Title,
    string Urgency,
    string Status,
    string UnitNumber,
    string BuildingName,
    int AssignedStaffMemberId,
    string AssignedStaffMemberName,
    DateTime? Eta,
    DateTime CreatedAt);

/// <summary>Staff availability entry for the dashboard summary widget.</summary>
public sealed record StaffAvailabilitySummaryDto(
    int StaffMemberId,
    string FullName,
    string? JobTitle,
    string Availability);
