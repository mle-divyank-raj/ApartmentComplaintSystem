using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Users.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(int UserId) : IRequest<Result<UserDto>>;

public sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public DeactivateUserCommandHandler(
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<UserDto>> Handle(
        DeactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        // Property scope enforcement: user must belong to the same property
        if (user is null || user.PropertyId != _propertyContext.PropertyId)
            return Result<UserDto>.Failure(
                new Error("User.NotFound", $"User with ID {command.UserId} was not found."));

        user.Deactivate();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDto>.Success(UserDto.FromDomain(user));
    }
}
