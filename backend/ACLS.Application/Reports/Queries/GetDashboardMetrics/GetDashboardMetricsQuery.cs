using ACLS.Application.Common.Interfaces;
using ACLS.Application.Reports.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Reporting;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Reports.Queries.GetDashboardMetrics;

public sealed record GetDashboardMetricsQuery : IRequest<Result<DashboardMetricsDto>>;

public sealed class GetDashboardMetricsQueryHandler
    : IRequestHandler<GetDashboardMetricsQuery, Result<DashboardMetricsDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetDashboardMetricsQueryHandler(
        IComplaintRepository complaintRepository,
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<DashboardMetricsDto>> Handle(
        GetDashboardMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var propertyId = _propertyContext.PropertyId;

        var allComplaints = await _complaintRepository.GetAllAsync(propertyId, null, cancellationToken);
        var allStaff = await _staffRepository.GetAllByPropertyAsync(propertyId, cancellationToken);

        var openCount = allComplaints.Count(c => c.Status == TicketStatus.OPEN);
        var assignedCount = allComplaints.Count(c => c.Status == TicketStatus.ASSIGNED);
        var inProgressCount = allComplaints.Count(c =>
            c.Status is TicketStatus.EN_ROUTE or TicketStatus.IN_PROGRESS);
        var resolvedCount = allComplaints.Count(c => c.Status == TicketStatus.RESOLVED);
        var closedCount = allComplaints.Count(c => c.Status == TicketStatus.CLOSED);
        var sosActiveCount = allComplaints.Count(c =>
            c.Urgency == Urgency.SOS_EMERGENCY &&
            c.Status is not TicketStatus.RESOLVED and not TicketStatus.CLOSED);

        // Build active assignments list
        var activeComplaints = allComplaints
            .Where(c => c.Status is TicketStatus.ASSIGNED or TicketStatus.EN_ROUTE or TicketStatus.IN_PROGRESS)
            .ToList();

        var activeAssignments = new List<ActiveAssignmentDto>();
        foreach (var complaint in activeComplaints)
        {
            if (complaint.AssignedStaffMemberId is null) continue;

            var staff = allStaff.FirstOrDefault(s => s.StaffMemberId == complaint.AssignedStaffMemberId);
            if (staff is null) continue;

            var staffUser = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
            var staffName = staffUser is null ? string.Empty : $"{staffUser.FirstName} {staffUser.LastName}";

            activeAssignments.Add(new ActiveAssignmentDto(
                complaint.ComplaintId,
                complaint.Title,
                complaint.Urgency.ToString(),
                complaint.Status.ToString(),
                string.Empty, // UnitNumber not available in Complaint aggregate
                string.Empty, // BuildingName not available in Complaint aggregate
                staff.StaffMemberId,
                staffName,
                complaint.Eta,
                complaint.CreatedAt));
        }

        // Build staff availability summary
        var staffSummary = new List<StaffAvailabilitySummaryDto>();
        foreach (var staff in allStaff)
        {
            var staffUser = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
            var fullName = staffUser is null ? string.Empty : $"{staffUser.FirstName} {staffUser.LastName}";
            staffSummary.Add(new StaffAvailabilitySummaryDto(
                staff.StaffMemberId, fullName, staff.JobTitle, staff.Availability.ToString()));
        }

        return Result<DashboardMetricsDto>.Success(new DashboardMetricsDto(
            openCount, assignedCount, inProgressCount, resolvedCount, closedCount, sosActiveCount,
            activeAssignments, staffSummary));
    }
}
