using FluentValidation;

namespace ACLS.Application.Complaints.Commands.ResolveComplaint;

public sealed class ResolveComplaintCommandValidator : AbstractValidator<ResolveComplaintCommand>
{
    public ResolveComplaintCommandValidator()
    {
        RuleFor(x => x.ComplaintId)
            .GreaterThan(0).WithMessage("ComplaintId must be a positive integer.");

        RuleForEach(x => x.CompletionPhotoUrls)
            .ChildRules(photo =>
            {
                photo.RuleFor(p => p.Url)
                    .NotEmpty().WithMessage("Completion photo URL must not be empty.");
                photo.RuleFor(p => p.Type)
                    .NotEmpty().WithMessage("Completion photo type must not be empty.");
            });
    }
}
