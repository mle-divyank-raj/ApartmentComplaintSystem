using ACLS.SharedKernel;
using ACLS.Domain.Complaints;

namespace ACLS.Domain.Dispatch;

/// <summary>
/// Input criteria for the Smart Dispatch algorithm.
/// Encapsulates the required skills and urgency weight derived from a Complaint.
/// </summary>
public sealed class DispatchCriteria : ValueObject
{
    /// <summary>
    /// Skills required to resolve the Complaint.
    /// Used to calculate SkillScore for each candidate StaffMember.
    /// If empty, all candidates receive SkillScore = 1.0.
    /// </summary>
    public IReadOnlyList<string> RequiredSkills { get; }

    /// <summary>
    /// Urgency weight multiplier applied to MatchScore.
    /// SOS_EMERGENCY = 2.0, all other urgency levels = 1.0.
    /// </summary>
    public double UrgencyWeight { get; }

    private DispatchCriteria(IReadOnlyList<string> requiredSkills, double urgencyWeight)
    {
        RequiredSkills = requiredSkills;
        UrgencyWeight = urgencyWeight;
    }

    /// <summary>
    /// Creates DispatchCriteria from a Complaint.
    /// UrgencyWeight is fixed: SOS_EMERGENCY → 2.0, all others → 1.0.
    /// </summary>
    public static DispatchCriteria FromComplaint(Complaint complaint)
    {
        Guard.Against.Null(complaint, nameof(complaint));

        var urgencyWeight = complaint.Urgency == Urgency.SOS_EMERGENCY ? 2.0 : 1.0;

        return new DispatchCriteria(
            complaint.RequiredSkills.AsReadOnly(),
            urgencyWeight);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        foreach (var skill in RequiredSkills)
            yield return skill;
        yield return UrgencyWeight;
    }
}
