using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using FluentValidation;
using MediatR;

namespace ACLS.Application.Complaints.Commands.AddWorkNote;

public sealed record AddWorkNoteCommand(
    int ComplaintId,
    string Content) : IRequest<Result<WorkNoteDto>>;

public sealed class AddWorkNoteCommandValidator : AbstractValidator<AddWorkNoteCommand>
{
    public AddWorkNoteCommandValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AddWorkNoteCommandHandler
    : IRequestHandler<AddWorkNoteCommand, Result<WorkNoteDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;

    public AddWorkNoteCommandHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<WorkNoteDto>> Handle(
        AddWorkNoteCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<WorkNoteDto>.Failure(ComplaintErrors.NotFound(command.ComplaintId));

        var workNote = WorkNote.Create(
            complaintId: command.ComplaintId,
            staffMemberId: _propertyContext.UserId,
            content: command.Content);

        await _complaintRepository.AddWorkNoteAsync(workNote, cancellationToken);

        return Result<WorkNoteDto>.Success(WorkNoteDto.FromDomain(workNote));
    }
}
