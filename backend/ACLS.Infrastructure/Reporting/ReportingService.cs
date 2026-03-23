using ACLS.Domain.Complaints;
using ACLS.Domain.Reporting;

namespace ACLS.Infrastructure.Reporting;

/// <summary>
/// Stub implementation of IReportingService for development.
/// Replace with real EF Core projections using AclsDbContext for production.
/// </summary>
public sealed class ReportingService : IReportingService
{
    public Task<IReadOnlyList<StaffPerformanceSummary>> GetStaffPerformanceAsync(
        int propertyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StaffPerformanceSummary>>(new List<StaffPerformanceSummary>());

    public Task<Dictionary<TicketStatus, int>> GetComplaintCountsByStatusAsync(
        int propertyId,
        CancellationToken ct) =>
        Task.FromResult(new Dictionary<TicketStatus, int>());

    public Task<decimal?> GetAverageTatAsync(
        int propertyId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct) =>
        Task.FromResult<decimal?>(null);
}
