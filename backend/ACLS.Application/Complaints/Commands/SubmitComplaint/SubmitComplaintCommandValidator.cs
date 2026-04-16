using ACLS.Domain.Complaints;
using FluentValidation;

namespace ACLS.Application.Complaints.Commands.SubmitComplaint;

/// <summary>
/// Validates SubmitComplaintCommand fields before the handler is invoked.
/// Urgency string must match a valid Urgency enum value.
/// Category must not be blank (domain derives RequiredSkills from it).
/// </summary>
public sealed class SubmitComplaintCommandValidator : AbstractValidator<SubmitComplaintCommand>
{
    private static readonly string[] ValidUrgencies =
        Enum.GetNames<Urgency>();

    public SubmitComplaintCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.Urgency)
            .NotEmpty().WithMessage("Urgency is required.")
            .Must(u => ValidUrgencies.Contains(u, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Urgency must be one of: {string.Join(", ", ValidUrgencies)}.");

        RuleForEach(x => x.MediaUrls)
            .ChildRules(media =>
            {
                media.RuleFor(m => m.Url)
                    .NotEmpty().WithMessage("Media URL must not be empty.");
                media.RuleFor(m => m.Type)
                    .NotEmpty().WithMessage("Media type must not be empty.");
            });
    }
}
