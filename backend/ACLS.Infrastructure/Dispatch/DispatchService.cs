using ACLS.Domain.Complaints;
using ACLS.Domain.Dispatch;
using ACLS.Domain.Staff;

namespace ACLS.Infrastructure.Dispatch;

/// <summary>
/// Implementation of IDispatchService.
/// Scores and ranks on-duty StaffMembers (AVAILABLE, BUSY, ON_BREAK) for a given Complaint using:
///   matchScore = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight × availabilityWeight
///
/// availabilityWeight: AVAILABLE=1.0, BUSY=0.6, ON_BREAK=0.3
/// This ensures a skill-matched BUSY plumber ranks above an available electrician with no matching skills.
///
/// This class is a pure algorithm — no EF Core, no HTTP, no external services.
/// It depends only on IStaffRepository (a Domain interface).
///
/// See: docs/07_Implementation/patterns/dispatch_algorithm.md for the full specification.
/// </summary>
public sealed class DispatchService : IDispatchService
{
    private readonly IStaffRepository _staffRepository;

    public DispatchService(IStaffRepository staffRepository)
        => _staffRepository = staffRepository;

    public async Task<List<StaffScore>> FindOptimalStaffAsync(
        Complaint complaint,
        CancellationToken ct)
    {
        var candidates = await _staffRepository.GetOnDutyAsync(complaint.PropertyId, ct);

        if (candidates.Count == 0)
            return [];

        var now = DateTime.UtcNow;

        var maxIdleTime = candidates
            .Select(s => now - (s.LastAssignedAt ?? DateTime.MinValue))
            .Max();

        var urgencyWeight = GetUrgencyWeight(complaint.Urgency);

        var scores = candidates
            .Select(staff =>
            {
                var skillScore        = CalculateSkillScore(staff, complaint);
                var idleScore         = CalculateIdleScore(staff, now, maxIdleTime);
                var availabilityWeight = GetAvailabilityWeight(staff.Availability);
                var matchScore        = (skillScore * DispatchWeights.SkillWeight
                                       + idleScore  * DispatchWeights.IdleWeight)
                                       * urgencyWeight
                                       * availabilityWeight;

                return new StaffScore(
                    staffMember: staff,
                    matchScore:  matchScore,
                    skillScore:  skillScore,
                    idleScore:   idleScore);
            })
            .OrderByDescending(s => s.MatchScore)
            .ToList();

        return scores;
    }

    private static double CalculateSkillScore(StaffMember staff, Complaint complaint)
    {
        if (complaint.RequiredSkills is null || complaint.RequiredSkills.Count == 0)
            return 1.0;

        var requiredSkills = complaint.RequiredSkills
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var staffSkills = staff.Skills
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var matchingCount = requiredSkills.Intersect(staffSkills).Count();

        return (double)matchingCount / requiredSkills.Count;
    }

    private static double CalculateIdleScore(
        StaffMember staff,
        DateTime now,
        TimeSpan maxIdleTime)
    {
        if (maxIdleTime == TimeSpan.Zero)
            return 1.0;

        var lastAssigned = staff.LastAssignedAt ?? DateTime.MinValue;
        var idleTime = now - lastAssigned;

        var clampedIdleTime = TimeSpan.FromTicks(
            Math.Max(0, Math.Min(idleTime.Ticks, maxIdleTime.Ticks)));

        return clampedIdleTime.TotalSeconds / maxIdleTime.TotalSeconds;
    }

    private static double GetUrgencyWeight(Urgency urgency)
        => urgency == Urgency.SOS_EMERGENCY
            ? DispatchWeights.SosUrgencyMultiplier
            : DispatchWeights.DefaultUrgencyMultiplier;

    private static double GetAvailabilityWeight(StaffState availability) => availability switch
    {
        StaffState.AVAILABLE => DispatchWeights.AvailabilityWeightAvailable,
        StaffState.BUSY      => DispatchWeights.AvailabilityWeightBusy,
        StaffState.ON_BREAK  => DispatchWeights.AvailabilityWeightOnBreak,
        _                    => 0.0
    };
}
