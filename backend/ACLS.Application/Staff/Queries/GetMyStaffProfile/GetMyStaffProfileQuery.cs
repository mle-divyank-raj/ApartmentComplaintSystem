using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Application.Staff.DTOs;
using ACLS.Application.Staff.Queries.GetAllStaff;
using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Staff.Queries.GetMyStaffProfile;

/// <summary>
/// Returns the authenticated staff member's profile along with their active complaint assignments.
/// </summary>
public sealed record GetMyStaffProfileQuery : IRequest<Result<StaffMemberWithAssignmentsDto>>;

public sealed record StaffMemberWithAssignmentsDto(
    StaffMemberDto Profile,
    IReadOnlyList<ComplaintDto> ActiveAssignments);

public sealed class GetMyStaffProfileQueryHandler
    : IRequestHandler<GetMyStaffProfileQuery, Result<StaffMemberWithAssignmentsDto>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetMyStaffProfileQueryHandler(
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<StaffMemberWithAssignmentsDto>> Handle(
        GetMyStaffProfileQuery query,
        CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByUserIdAsync(
            _propertyContext.UserId, _propertyContext.PropertyId, cancellationToken);

        if (staff is null)
            return Result<StaffMemberWithAssignmentsDto>.Failure(
                new Error("Staff.NotFound", "Staff member profile not found for the current user."));

        var user = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
        var fullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}";

        var assignments = await _complaintRepository.GetByStaffMemberAsync(
            staff.StaffMemberId, _propertyContext.PropertyId, cancellationToken);

        var activeAssignments = assignments
            .Where(c => c.Status is TicketStatus.ASSIGNED or TicketStatus.EN_ROUTE or TicketStatus.IN_PROGRESS)
            .Select(ComplaintDto.FromDomain)
            .ToList();

        return Result<StaffMemberWithAssignmentsDto>.Success(new StaffMemberWithAssignmentsDto(
            GetAllStaffQueryHandler.ToDto(staff, fullName),
            activeAssignments));
    }
}
