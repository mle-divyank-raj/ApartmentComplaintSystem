using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Users.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(int UserId) : IRequest<Result<UserDto>>;

public sealed class ReactivateUserCommandHandler
    : IRequestHandler<ReactivateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public ReactivateUserCommandHandler(
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<UserDto>> Handle(
        ReactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || user.PropertyId != _propertyContext.PropertyId)
            return Result<UserDto>.Failure(
                new Error("User.NotFound", $"User with ID {command.UserId} was not found."));

        user.Reactivate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDto>.Success(UserDto.FromDomain(user));
    }
}
