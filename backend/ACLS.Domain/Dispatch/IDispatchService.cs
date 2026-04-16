using ACLS.Domain.Complaints;

namespace ACLS.Domain.Dispatch;

/// <summary>
/// Smart Dispatch service interface. Ranks on-duty StaffMembers (AVAILABLE, BUSY, ON_BREAK)
/// for a given Complaint using the formula:
///
///   skillScore    = count(intersection(staff.Skills, complaint.RequiredSkills))
///                   ÷ count(complaint.RequiredSkills)  [1.0 if RequiredSkills empty]
///
///   idleScore     = Normalise(DateTime.UtcNow - staff.LastAssignedAt)
///                   [0.0–1.0 against max idle time in candidate pool]
///
///   urgencyWeight      = SOS_EMERGENCY ? 2.0 : 1.0
///   availabilityWeight = AVAILABLE ? 1.0 : BUSY ? 0.6 : ON_BREAK ? 0.3
///
///   matchScore    = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight × availabilityWeight
///
/// Returns List{StaffScore} ordered by matchScore DESCENDING.
/// OFF_DUTY staff are excluded entirely.
/// The dispatch service ranks candidates — it does NOT make assignment decisions.
/// Defined in Domain; implemented in ACLS.Infrastructure.Dispatch.DispatchService.
/// </summary>
public interface IDispatchService
{
    /// <summary>
    /// Returns a ranked list of on-duty StaffMembers (AVAILABLE, BUSY, ON_BREAK) for the given Complaint.
    /// Queries staff scoped to complaint.PropertyId, excluding OFF_DUTY.
    /// Returns an empty list if no on-duty staff exist for the Property.
    /// </summary>
    Task<List<StaffScore>> FindOptimalStaffAsync(
        Complaint complaint,
        CancellationToken ct);
}
