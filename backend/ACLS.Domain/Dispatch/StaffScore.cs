using ACLS.SharedKernel;
using ACLS.Domain.Staff;

namespace ACLS.Domain.Dispatch;

/// <summary>
/// Output unit of IDispatchService. Pairs a StaffMember candidate with their computed scores.
/// The dispatch service returns a ranked List{StaffScore} — it does NOT make assignment decisions.
/// The Manager reviews the ranked list and makes the final assignment decision.
/// </summary>
public sealed class StaffScore : ValueObject
{
    /// <summary>The candidate StaffMember.</summary>
    public StaffMember StaffMember { get; }

    /// <summary>
    /// Final composite score: (SkillScore × 0.6 + IdleScore × 0.4) × UrgencyWeight.
    /// Higher is better. Used for ordering only — never displayed as a raw number to the Manager.
    /// </summary>
    public double MatchScore { get; }

    /// <summary>
    /// Proportion of RequiredSkills the StaffMember possesses.
    /// Range: 0.0–1.0. If complaint has no RequiredSkills, all candidates receive 1.0.
    /// </summary>
    public double SkillScore { get; }

    /// <summary>
    /// Normalised idle time. 0.0 = just freed, 1.0 = idle longest in the candidate pool.
    /// </summary>
    public double IdleScore { get; }

    public StaffScore(StaffMember staffMember, double matchScore, double skillScore, double idleScore)
    {
        Guard.Against.Null(staffMember, nameof(staffMember));
        StaffMember = staffMember;
        MatchScore = matchScore;
        SkillScore = skillScore;
        IdleScore = idleScore;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return StaffMember.StaffMemberId;
        yield return MatchScore;
    }
}
