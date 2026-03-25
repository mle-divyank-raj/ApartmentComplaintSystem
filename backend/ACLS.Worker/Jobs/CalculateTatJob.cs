using ACLS.Domain.Complaints;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.Jobs;

/// <summary>
/// Calculates and stores the Turn-Around Time (TAT) for a resolved Complaint.
/// TAT formula: (ResolvedAt − CreatedAt).TotalMinutes stored as decimal in Complaint.Tat.
/// Called by ComplaintResolvedEventHandler immediately after the resolution domain event.
/// </summary>
public sealed class CalculateTatJob
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ILogger<CalculateTatJob> _logger;

    public CalculateTatJob(
        IComplaintRepository complaintRepository,
        ILogger<CalculateTatJob> logger)
    {
        _complaintRepository = complaintRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(int complaintId, int propertyId, CancellationToken ct)
    {
        _logger.LogInformation(
            "CalculateTatJob: Starting for ComplaintId {ComplaintId} | PropertyId {PropertyId}",
            complaintId, propertyId);

        var complaint = await _complaintRepository.GetByIdAsync(complaintId, propertyId, ct);

        if (complaint is null)
        {
            _logger.LogWarning(
                "CalculateTatJob: ComplaintId {ComplaintId} not found for PropertyId {PropertyId}; TAT skipped",
                complaintId, propertyId);
            return;
        }

        if (complaint.ResolvedAt is null)
        {
            _logger.LogWarning(
                "CalculateTatJob: ComplaintId {ComplaintId} has no ResolvedAt; TAT cannot be calculated",
                complaintId);
            return;
        }

        var tatMinutes = (decimal)(complaint.ResolvedAt.Value - complaint.CreatedAt).TotalMinutes;

        complaint.RecordTat(tatMinutes);
        await _complaintRepository.UpdateAsync(complaint, ct);

        _logger.LogInformation(
            "CalculateTatJob: TAT recorded for ComplaintId {ComplaintId} | TAT: {TatMinutes:F2} minutes",
            complaintId, tatMinutes);
    }
}
