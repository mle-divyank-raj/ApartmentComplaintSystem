using ACLS.Application.Common.Interfaces;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Complaints.Commands.UpdateEta;

public sealed record UpdateEtaCommand(
    int ComplaintId,
    DateTime Eta) : IRequest<Result>;

public sealed class UpdateEtaCommandValidator : AbstractValidator<UpdateEtaCommand>
{
    public UpdateEtaCommandValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Eta).GreaterThan(DateTime.UtcNow).WithMessage("ETA must be a future UTC date.");
    }
}

public sealed class UpdateEtaCommandHandler : IRequestHandler<UpdateEtaCommand, Result>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public UpdateEtaCommandHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result> Handle(UpdateEtaCommand command, CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result.Failure(ComplaintErrors.NotFound(command.ComplaintId));

        var result = complaint.UpdateEta(command.Eta);
        if (result.IsFailure)
            return result;

        await _complaintRepository.UpdateAsync(complaint, cancellationToken);
        return Result.Success();
    }
}
