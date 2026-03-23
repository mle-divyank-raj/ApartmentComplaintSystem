using System.Reflection;
using ACLS.Domain.Staff;

namespace Domain.Tests;

/// <summary>
/// Reflection-based test extension methods for domain entities that have private setters.
/// These helpers exist ONLY in the test project — never in production code.
/// Pattern: use reflection to set test state, mirroring how EF Core populates private-set properties.
/// </summary>
internal static class DomainTestExtensions
{
    /// <summary>
    /// Sets StaffMember.LastAssignedAt to a specific value for test scenarios.
    /// Necessary because LastAssignedAt has a private setter and MarkBusy() only sets DateTime.UtcNow.
    /// </summary>
    internal static void SetLastAssignedAtForTesting(
        this StaffMember staff,
        DateTime lastAssignedAt)
    {
        var property = typeof(StaffMember).GetProperty(
            "LastAssignedAt",
            BindingFlags.Public | BindingFlags.Instance);

        property!.SetValue(staff, lastAssignedAt);
    }
}
