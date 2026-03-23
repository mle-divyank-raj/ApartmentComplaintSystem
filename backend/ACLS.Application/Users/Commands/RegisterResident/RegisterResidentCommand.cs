using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Users.Commands.RegisterResident;

public sealed record RegisterResidentCommand(
    string InvitationToken,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone) : IRequest<Result<AuthTokenDto>>;

public sealed class RegisterResidentCommandValidator : AbstractValidator<RegisterResidentCommand>
{
    public RegisterResidentCommandValidator()
    {
        RuleFor(x => x.InvitationToken).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterResidentCommandHandler
    : IRequestHandler<RegisterResidentCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IResidentRepository _residentRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterResidentCommandHandler(
        IUserRepository userRepository,
        IResidentRepository residentRepository,
        IPropertyRepository propertyRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _residentRepository = residentRepository;
        _propertyRepository = propertyRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthTokenDto>> Handle(
        RegisterResidentCommand command,
        CancellationToken cancellationToken)
    {
        // Validate the invitation token
        var invitation = await _userRepository.GetInvitationTokenAsync(
            command.InvitationToken, cancellationToken);

        if (invitation is null || invitation.IsRevoked || invitation.UsedAt.HasValue)
            return Result<AuthTokenDto>.Failure(
                new Error("Auth.InvalidInvitationToken", "The invitation token is invalid or has already been used."));

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return Result<AuthTokenDto>.Failure(
                new Error("Auth.TokenExpired", "The invitation token has expired."));

        // Check email uniqueness
        var existing = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existing is not null)
            return Result<AuthTokenDto>.Failure(
                new Error("Auth.EmailAlreadyRegistered", "This email address is already registered."));

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = User.Create(
            email: command.Email,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            role: Role.Resident,
            propertyId: invitation.PropertyId,
            phone: command.Phone);

        await _userRepository.AddAsync(user, cancellationToken);

        var resident = Resident.Create(userId: user.UserId, unitId: invitation.UnitId);
        await _residentRepository.AddAsync(resident, cancellationToken);

        // Mark token as redeemed
        invitation.Redeem(user.UserId, DateTime.UtcNow);
        await _userRepository.UpdateInvitationTokenAsync(invitation, cancellationToken);

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return Result<AuthTokenDto>.Success(new AuthTokenDto(
            token, expiresAt, user.UserId, user.Role.ToString(), user.PropertyId));
    }
}
