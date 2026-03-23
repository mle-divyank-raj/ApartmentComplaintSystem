using ACLS.Application.Common.Interfaces;
using ACLS.Application.Staff.DTOs;
using ACLS.Application.Staff.Queries.GetAllStaff;
using ACLS.Domain.Identity;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Staff.Commands.UpdateAvailability;

public sealed record UpdateAvailabilityCommand(
    int StaffMemberId,
    string Availability) : IRequest<Result<StaffMemberDto>>;

public sealed class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    private static readonly HashSet<string> AllowedValues =
        new(StringComparer.OrdinalIgnoreCase) { "AVAILABLE", "ON_BREAK", "OFF_DUTY" };

    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(x => x.StaffMemberId).GreaterThan(0);
        RuleFor(x => x.Availability)
            .NotEmpty()
            .Must(v => AllowedValues.Contains(v))
            .WithMessage("Availability must be one of: AVAILABLE, ON_BREAK, OFF_DUTY. BUSY cannot be set manually.");
    }
}

public sealed class UpdateAvailabilityCommandHandler
    : IRequestHandler<UpdateAvailabilityCommand, Result<StaffMemberDto>>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public UpdateAvailabilityCommandHandler(
        IStaffRepository staffRepository,
        IUserRepository userRepository,
        ICurrentPropertyContext propertyContext)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<StaffMemberDto>> Handle(
        UpdateAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var staff = await _staffRepository.GetByIdAsync(
            command.StaffMemberId, _propertyContext.PropertyId, cancellationToken);

        if (staff is null)
            return Result<StaffMemberDto>.Failure(
                new Error("Staff.NotFound", $"Staff member with ID {command.StaffMemberId} was not found."));

        // Staff can only update their own availability
        if (staff.UserId != _propertyContext.UserId)
            return Result<StaffMemberDto>.Failure(
                new Error("Staff.AccessDenied", "You can only update your own availability status."));

        if (!Enum.TryParse<StaffState>(command.Availability, ignoreCase: true, out var newState))
            return Result<StaffMemberDto>.Failure(
                new Error("Staff.InvalidAvailability", $"'{command.Availability}' is not a valid availability status."));

        staff.UpdateAvailability(newState);
        await _staffRepository.UpdateAsync(staff, cancellationToken);

        var user = await _userRepository.GetByIdAsync(staff.UserId, cancellationToken);
        var fullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}";

        return Result<StaffMemberDto>.Success(GetAllStaffQueryHandler.ToDto(staff, fullName));
    }
}
