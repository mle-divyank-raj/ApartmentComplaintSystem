using ACLS.Domain.Complaints;

namespace ACLS.Domain.Reporting;

/// <summary>
/// Reporting service interface. Provides pre-aggregated read models for the Manager Dashboard.
/// Implemented in ACLS.Infrastructure.Reporting — may use raw LINQ or projected EF Core queries.
/// Defined in Domain; implemented in ACLS.Infrastructure.
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Returns performance summaries for all StaffMembers of a Property within a date range.
    /// </summary>
    Task<IReadOnlyList<StaffPerformanceSummary>> GetStaffPerformanceAsync(
        int propertyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct);

    /// <summary>
    /// Returns the count of complaints grouped by TicketStatus for the Property.
    /// Used for the Manager Dashboard summary cards.
    /// </summary>
    Task<Dictionary<TicketStatus, int>> GetComplaintCountsByStatusAsync(
        int propertyId,
        CancellationToken ct);

    /// <summary>
    /// Returns the average TAT (minutes) across all RESOLVED complaints for the Property within the date range.
    /// </summary>
    Task<decimal?> GetAverageTatAsync(
        int propertyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct);
}
