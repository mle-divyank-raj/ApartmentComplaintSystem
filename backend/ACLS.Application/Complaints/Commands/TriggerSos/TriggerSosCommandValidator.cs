using FluentValidation;

namespace ACLS.Application.Complaints.Commands.TriggerSos;

public sealed class TriggerSosCommandValidator : AbstractValidator<TriggerSosCommand>
{
    public TriggerSosCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("UnitId must be a positive integer.");
    }
}
