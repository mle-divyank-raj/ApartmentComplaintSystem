using ACLS.Application.Common.Interfaces;
using ACLS.Application.Staff.DTOs;
using ACLS.Application.Staff.Queries.GetAllStaff;
using ACLS.Domain.Identity;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Staff.Queries.GetAvailableStaff;

public sealed record GetAvailableStaffQuery : IRequest<Result<IReadOnlyList<StaffMemberDto>>>;

public sealed class GetAvailableStaffQueryHandler
    : IRequestHandler<GetAvailableStaffQuery, Result<IReadOnlyList<StaffMemberDto>>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAvailableStaffQueryHandler(
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<StaffMemberDto>>> Handle(
        GetAvailableStaffQuery query,
        CancellationToken cancellationToken)
    {
        var staffMembers = await _staffRepository.GetAvailableAsync(
            _propertyContext.PropertyId, cancellationToken);

        var dtos = new List<StaffMemberDto>(staffMembers.Count);
        foreach (var staff in staffMembers)
        {
            var user = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
            var fullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}";
            dtos.Add(GetAllStaffQueryHandler.ToDto(staff, fullName));
        }

        return Result<IReadOnlyList<StaffMemberDto>>.Success(dtos);
    }
}
