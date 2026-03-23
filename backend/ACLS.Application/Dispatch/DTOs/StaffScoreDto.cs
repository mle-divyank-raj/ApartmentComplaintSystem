using ACLS.Domain.Dispatch;

namespace ACLS.Application.Dispatch.DTOs;

/// <summary>
/// DTO returned from the GetDispatchRecommendations query. Contains the ranked staff score
/// produced by the dispatch algorithm for a given Complaint.
/// </summary>
public sealed record StaffScoreDto(
    int StaffMemberId,
    int UserId,
    string? JobTitle,
    List<string> Skills,
    double MatchScore,
    double SkillScore,
    double IdleScore)
{
    public static StaffScoreDto FromDomain(StaffScore score) => new(
        score.StaffMember.StaffMemberId,
        score.StaffMember.UserId,
        score.StaffMember.JobTitle,
        score.StaffMember.Skills,
        score.MatchScore,
        score.SkillScore,
        score.IdleScore);
}
