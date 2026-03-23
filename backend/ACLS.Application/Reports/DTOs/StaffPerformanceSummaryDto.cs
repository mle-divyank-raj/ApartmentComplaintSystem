using ACLS.Domain.Reporting;

namespace ACLS.Application.Reports.DTOs;

/// <summary>
/// DTO wrapping the domain StaffPerformanceSummary read model for API responses.
/// </summary>
public sealed record StaffPerformanceSummaryDto(
    int StaffMemberId,
    string FullName,
    string? JobTitle,
    int TotalResolved,
    double? AverageRating,
    double? AverageTatMinutes)
{
    public static StaffPerformanceSummaryDto FromDomain(StaffPerformanceSummary summary, string? jobTitle) => new(
        summary.StaffMemberId,
        summary.FullName,
        jobTitle,
        summary.TotalComplaintsResolved,
        summary.AverageRating.HasValue ? (double)summary.AverageRating.Value : null,
        summary.AverageTatMinutes.HasValue ? (double)summary.AverageTatMinutes.Value : null);
}
