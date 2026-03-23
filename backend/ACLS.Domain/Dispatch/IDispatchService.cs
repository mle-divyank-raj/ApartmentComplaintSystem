using ACLS.Domain.Complaints;

namespace ACLS.Domain.Dispatch;

/// <summary>
/// Smart Dispatch service interface. Ranks available StaffMembers for a given Complaint
/// using the fixed formula defined in ai_collaboration_rules.md Rule 9:
///
///   skillScore    = count(intersection(staff.Skills, complaint.RequiredSkills))
///                   ÷ count(complaint.RequiredSkills)  [1.0 if RequiredSkills empty]
///
///   idleScore     = Normalise(DateTime.UtcNow - staff.LastAssignedAt)
///                   [0.0–1.0 against max idle time in candidate pool]
///
///   urgencyWeight = SOS_EMERGENCY ? 2.0 : 1.0
///
///   matchScore    = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight
///
/// Returns List{StaffScore} ordered by matchScore DESCENDING.
/// The dispatch service ranks candidates — it does NOT make assignment decisions.
/// Defined in Domain; implemented in ACLS.Infrastructure.Dispatch.DispatchService.
/// </summary>
public interface IDispatchService
{
    /// <summary>
    /// Returns a ranked list of available StaffMembers for the given Complaint.
    /// Queries only AVAILABLE staff scoped to complaint.PropertyId.
    /// Returns an empty list if no AVAILABLE staff exist for the Property.
    /// </summary>
    Task<List<StaffScore>> FindOptimalStaffAsync(
        Complaint complaint,
        CancellationToken ct);
}
