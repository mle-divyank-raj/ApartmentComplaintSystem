using System.Security.Cryptography;
using ACLS.Application.Common.Interfaces;
using ACLS.Application.Users.DTOs;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Users.Commands.InviteResident;

public sealed record InviteResidentCommand(
    string Email,
    int UnitId) : IRequest<Result<InvitationDto>>;

public sealed class InviteResidentCommandValidator : AbstractValidator<InviteResidentCommand>
{
    public InviteResidentCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UnitId).GreaterThan(0);
    }
}

public sealed class InviteResidentCommandHandler
    : IRequestHandler<InviteResidentCommand, Result<InvitationDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public InviteResidentCommandHandler(
        IUserRepository userRepository,
        IPropertyRepository propertyRepository,
        ICurrentPropertyContext propertyContext)
    {
        _userRepository = userRepository;
        _propertyRepository = propertyRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<InvitationDto>> Handle(
        InviteResidentCommand command,
        CancellationToken cancellationToken)
    {
        var unit = await _propertyRepository.GetUnitByIdAsync(command.UnitId, cancellationToken);
        if (unit is null)
            return Result<InvitationDto>.Failure(
                new Error("Unit.NotFound", $"Unit with ID {command.UnitId} was not found."));

        // Generate a cryptographically random 64-char hex token
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenString = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var invitation = InvitationToken.Create(
            token: tokenString,
            unitId: command.UnitId,
            propertyId: _propertyContext.PropertyId,
            issuedByManagerUserId: _propertyContext.UserId);

        await _userRepository.AddInvitationTokenAsync(invitation, cancellationToken);

        return Result<InvitationDto>.Success(new InvitationDto(
            invitation.InvitationTokenId,
            command.Email,
            command.UnitId,
            unit.UnitNumber,
            invitation.ExpiresAt,
            invitation.IssuedAt));
    }
}
