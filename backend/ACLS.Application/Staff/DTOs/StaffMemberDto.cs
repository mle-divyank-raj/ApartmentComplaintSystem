using ACLS.Domain.Staff;

namespace ACLS.Application.Staff.DTOs;

/// <summary>
/// DTO for a StaffMember returned by staff queries.
/// Full name is derived by joining StaffMember → User.
/// </summary>
public sealed record StaffMemberDto(
    int StaffMemberId,
    int UserId,
    string FullName,
    string? JobTitle,
    List<string> Skills,
    string Availability,
    double? AverageRating,
    DateTime? LastAssignedAt);
