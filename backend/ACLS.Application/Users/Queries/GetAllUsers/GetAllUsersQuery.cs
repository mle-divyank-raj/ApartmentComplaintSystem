using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public GetAllUsersQueryHandler(
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(
        GetAllUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllByPropertyAsync(
            _propertyContext.PropertyId, cancellationToken);

        var dtos = users.Select(UserDto.FromDomain).ToList();
        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }
}
