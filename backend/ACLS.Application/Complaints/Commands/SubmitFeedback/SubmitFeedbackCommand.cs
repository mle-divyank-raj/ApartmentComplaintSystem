using ACLS.Application.Common.Interfaces;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Complaints.Commands.SubmitFeedback;

public sealed record SubmitFeedbackCommand(
    int ComplaintId,
    int Rating,
    string? Comment) : IRequest<Result>;

public sealed class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Rating)
            .InclusiveBetween(ComplaintConstants.MinRating, ComplaintConstants.MaxRating)
            .WithMessage($"Rating must be between {ComplaintConstants.MinRating} and {ComplaintConstants.MaxRating}.");
        RuleFor(x => x.Comment)
            .MaximumLength(1000).When(x => x.Comment is not null)
            .WithMessage("Comment must not exceed 1000 characters.");
    }
}

public sealed class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, Result>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public SubmitFeedbackCommandHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result> Handle(SubmitFeedbackCommand command, CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result.Failure(ComplaintErrors.NotFound(command.ComplaintId));

        var result = complaint.Close(command.Rating, command.Comment);
        if (result.IsFailure)
            return result;

        await _complaintRepository.UpdateAsync(complaint, cancellationToken);
        return Result.Success();
    }
}
