using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Complaints.Commands.UpdateStatus;

/// <summary>
/// Command for staff-initiated status transitions:
///   ASSIGNED → EN_ROUTE    (staff accepts assignment and heads to unit)
///   EN_ROUTE  → IN_PROGRESS (staff arrives and begins work)
/// </summary>
public sealed record UpdateStatusCommand(
    int ComplaintId,
    string Status) : IRequest<Result<ComplaintDto>>;

public sealed class UpdateStatusCommandValidator : AbstractValidator<UpdateStatusCommand>
{
    public UpdateStatusCommandValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Status)
            .Must(s => s == "EN_ROUTE" || s == "IN_PROGRESS")
            .WithMessage("Status must be EN_ROUTE or IN_PROGRESS.");
    }
}

public sealed class UpdateStatusCommandHandler
    : IRequestHandler<UpdateStatusCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public UpdateStatusCommandHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext,
        IPublisher publisher)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
        _publisher = publisher;
    }

    public async Task<Result<ComplaintDto>> Handle(
        UpdateStatusCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<ComplaintDto>.Failure(
                ComplaintErrors.NotFound(command.ComplaintId));

        var transitionResult = command.Status switch
        {
            "EN_ROUTE"    => complaint.AcceptAssignment(),
            "IN_PROGRESS" => complaint.StartWork(),
            _             => Result.Failure(new Error(
                                "Complaint.InvalidStatus",
                                $"'{command.Status}' is not a supported status transition."))
        };

        if (transitionResult.IsFailure)
            return Result<ComplaintDto>.Failure(transitionResult.Error);

        await _complaintRepository.UpdateAsync(complaint, cancellationToken);

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
