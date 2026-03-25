using ACLS.Domain.Complaints;
using ACLS.Domain.Staff;
using Microsoft.Extensions.Logging;

namespace ACLS.Worker.Jobs;

/// <summary>
/// Recalculates and stores the AverageRating for a StaffMember based on all ResidentRating
/// values from complaints they have resolved. Called by ComplaintResolvedEventHandler and
/// FeedbackSubmittedEventHandler so the rating reflects the most recent feedback.
/// </summary>
public sealed class UpdateAverageRatingJob
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ILogger<UpdateAverageRatingJob> _logger;

    public UpdateAverageRatingJob(
        IComplaintRepository complaintRepository,
        IStaffRepository staffRepository,
        ILogger<UpdateAverageRatingJob> logger)
    {
        _complaintRepository = complaintRepository;
        _staffRepository = staffRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(int staffMemberId, int propertyId, CancellationToken ct)
    {
        _logger.LogInformation(
            "UpdateAverageRatingJob: Starting for StaffMemberId {StaffMemberId} | PropertyId {PropertyId}",
            staffMemberId, propertyId);

        var staffComplaints = await _complaintRepository.GetByStaffMemberAsync(
            staffMemberId, propertyId, ct);

        var ratings = staffComplaints
            .Where(c => c.ResidentRating.HasValue)
            .Select(c => c.ResidentRating!.Value)
            .ToList();

        if (ratings.Count == 0)
        {
            _logger.LogInformation(
                "UpdateAverageRatingJob: No rated complaints found for StaffMemberId {StaffMemberId}; skipping",
                staffMemberId);
            return;
        }

        var averageRating = (decimal)ratings.Average();

        var staff = await _staffRepository.GetByIdAsync(staffMemberId, propertyId, ct);

        if (staff is null)
        {
            _logger.LogWarning(
                "UpdateAverageRatingJob: StaffMemberId {StaffMemberId} not found for PropertyId {PropertyId}; skipping",
                staffMemberId, propertyId);
            return;
        }

        staff.UpdateAverageRating(averageRating);
        await _staffRepository.UpdateAsync(staff, ct);

        _logger.LogInformation(
            "UpdateAverageRatingJob: AverageRating updated | StaffMemberId {StaffMemberId} | Rating: {Rating:F2} | Based on {Count} feedback(s)",
            staffMemberId, averageRating, ratings.Count);
    }
}
