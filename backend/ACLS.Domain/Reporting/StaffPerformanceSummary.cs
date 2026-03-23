namespace ACLS.Domain.Reporting;

/// <summary>
/// Read model representing staff performance metrics for a date range.
/// Returned by IReportingService — never persisted directly to the DB.
/// </summary>
public sealed class StaffPerformanceSummary
{
    public int StaffMemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalComplaintsAssigned { get; set; }
    public int TotalComplaintsResolved { get; set; }
    public decimal? AverageTatMinutes { get; set; }
    public decimal? AverageRating { get; set; }
    public int SosComplaintsHandled { get; set; }
}
