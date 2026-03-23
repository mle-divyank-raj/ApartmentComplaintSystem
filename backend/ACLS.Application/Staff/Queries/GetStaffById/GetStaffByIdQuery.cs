using ACLS.Application.Common.Interfaces;
using ACLS.Application.Staff.DTOs;
using ACLS.Application.Staff.Queries.GetAllStaff;
using ACLS.Domain.Identity;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Staff.Queries.GetStaffById;

public sealed record GetStaffByIdQuery(int StaffMemberId)
    : IRequest<Result<StaffMemberDto>>;

public sealed class GetStaffByIdQueryHandler
    : IRequestHandler<GetStaffByIdQuery, Result<StaffMemberDto>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetStaffByIdQueryHandler(
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<StaffMemberDto>> Handle(
        GetStaffByIdQuery query,
        CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(
            query.StaffMemberId, _propertyContext.PropertyId, cancellationToken);

        if (staff is null)
            return Result<StaffMemberDto>.Failure(
                new Error("Staff.NotFound", $"Staff member with ID {query.StaffMemberId} was not found."));

        var user = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
        var fullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}";

        return Result<StaffMemberDto>.Success(GetAllStaffQueryHandler.ToDto(staff, fullName));
    }
}
