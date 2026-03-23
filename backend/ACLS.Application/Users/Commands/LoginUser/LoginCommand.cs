using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Users.Commands.LoginUser;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthTokenDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

        // Return the same error for both "not found" and "wrong password" to prevent email enumeration
        if (user is null || !_passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result<AuthTokenDto>.Failure(
                new Error("Auth.InvalidCredentials", "Email or password is incorrect."));

        if (!user.IsActive)
            return Result<AuthTokenDto>.Failure(
                new Error("Auth.AccountDeactivated", "This account has been deactivated. Contact your property manager."));

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return Result<AuthTokenDto>.Success(new AuthTokenDto(
            token, expiresAt, user.UserId, user.Role.ToString(), user.PropertyId));
    }
}
