using FluentValidation;

namespace ACLS.Application.Complaints.Commands.AssignComplaint;

public sealed class AssignComplaintCommandValidator : AbstractValidator<AssignComplaintCommand>
{
    public AssignComplaintCommandValidator()
    {
        RuleFor(x => x.ComplaintId)
            .GreaterThan(0).WithMessage("ComplaintId must be a positive integer.");

        RuleFor(x => x.StaffMemberId)
            .GreaterThan(0).WithMessage("StaffMemberId must be a positive integer.");
    }
}
