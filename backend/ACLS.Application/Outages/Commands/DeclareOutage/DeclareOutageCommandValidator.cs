using ACLS.Domain.Outages;
using FluentValidation;

namespace ACLS.Application.Outages.Commands.DeclareOutage;

public sealed class DeclareOutageCommandValidator : AbstractValidator<DeclareOutageCommand>
{
    private static readonly string[] ValidOutageTypes = Enum.GetNames<OutageType>();

    public DeclareOutageCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.OutageType)
            .NotEmpty().WithMessage("OutageType is required.")
            .Must(t => ValidOutageTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"OutageType must be one of: {string.Join(", ", ValidOutageTypes)}.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .When(x => x.EndTime.HasValue)
            .WithMessage("EndTime must be after StartTime.");
    }
}
