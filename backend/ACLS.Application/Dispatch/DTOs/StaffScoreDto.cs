using ACLS.Domain.Dispatch;

namespace ACLS.Application.Dispatch.DTOs;

/// <summary>
/// DTO returned from the GetDispatchRecommendations query. Contains the ranked staff score
/// produced by the dispatch algorithm for a given Complaint.
/// </summary>
public sealed record StaffScoreDto(
    int StaffMemberId,
    int UserId,
    string FullName,
    string? JobTitle,
    List<string> Skills,
    string Availability,
    double MatchScore,
    double SkillScore,
    double IdleScore,
    decimal? AverageRating,
    DateTime? LastAssignedAt)
{
    public static StaffScoreDto FromDomain(StaffScore score, string fullName) => new(
        score.StaffMember.StaffMemberId,
        score.StaffMember.UserId,
        fullName,
        score.StaffMember.JobTitle,
        score.StaffMember.Skills,
        score.StaffMember.Availability.ToString(),
        score.MatchScore,
        score.SkillScore,
        score.IdleScore,
        score.StaffMember.AverageRating,
        score.StaffMember.LastAssignedAt);
}
