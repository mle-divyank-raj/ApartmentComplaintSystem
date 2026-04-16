namespace ACLS.Domain.Dispatch;

/// <summary>
/// Named constants for the Smart Dispatch scoring algorithm.
/// Never use magic numbers inline — reference these constants everywhere.
/// Formula: matchScore = (skillScore × SkillWeight + idleScore × IdleWeight) × urgencyWeight × availabilityWeight
/// </summary>
public static class DispatchWeights
{
    /// <summary>Weight applied to SkillScore in the matchScore formula. Must sum to 1.0 with IdleWeight.</summary>
    public const double SkillWeight = 0.6;

    /// <summary>Weight applied to IdleScore in the matchScore formula. Must sum to 1.0 with SkillWeight.</summary>
    public const double IdleWeight = 0.4;

    /// <summary>Urgency multiplier applied when Complaint.Urgency == SOS_EMERGENCY.</summary>
    public const double SosUrgencyMultiplier = 2.0;

    /// <summary>Urgency multiplier applied for all non-SOS urgency levels (HIGH, MEDIUM, LOW).</summary>
    public const double DefaultUrgencyMultiplier = 1.0;

    /// <summary>Availability multiplier for AVAILABLE staff — no penalty.</summary>
    public const double AvailabilityWeightAvailable = 1.0;

    /// <summary>Availability multiplier for BUSY staff — partial penalty (currently working but can be assigned).</summary>
    public const double AvailabilityWeightBusy = 0.6;

    /// <summary>Availability multiplier for ON_BREAK staff — lower priority than AVAILABLE/BUSY.</summary>
    public const double AvailabilityWeightOnBreak = 0.3;
}
