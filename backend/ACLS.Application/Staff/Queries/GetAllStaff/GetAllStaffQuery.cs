using ACLS.Application.Common.Interfaces;
using ACLS.Application.Staff.DTOs;
using ACLS.Domain.Identity;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Staff.Queries.GetAllStaff;

public sealed record GetAllStaffQuery : IRequest<Result<IReadOnlyList<StaffMemberDto>>>;

public sealed class GetAllStaffQueryHandler
    : IRequestHandler<GetAllStaffQuery, Result<IReadOnlyList<StaffMemberDto>>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAllStaffQueryHandler(
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<StaffMemberDto>>> Handle(
        GetAllStaffQuery query,
        CancellationToken cancellationToken)
    {
        var staffMembers = await _staffRepository.GetAllByPropertyAsync(
            _propertyContext.PropertyId, cancellationToken);

        var dtos = new List<StaffMemberDto>(staffMembers.Count);
        foreach (var staff in staffMembers)
        {
            var user = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
            var fullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}";
            dtos.Add(ToDto(staff, fullName));
        }

        return Result<IReadOnlyList<StaffMemberDto>>.Success(dtos);
    }

    internal static StaffMemberDto ToDto(StaffMember staff, string fullName) => new(
        staff.StaffMemberId,
        staff.UserId,
        fullName,
        staff.JobTitle,
        staff.Skills,
        staff.Availability.ToString(),
        staff.AverageRating.HasValue ? (double)staff.AverageRating.Value : null,
        staff.LastAssignedAt);
}
