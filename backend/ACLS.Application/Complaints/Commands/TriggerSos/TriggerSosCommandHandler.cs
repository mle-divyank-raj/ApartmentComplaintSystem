using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.TriggerSos;

/// <summary>
/// Handles SOS emergency complaint creation.
/// Creates the complaint, calls TriggerSos() to force SOS_EMERGENCY + ASSIGNED status,
/// then persists. ACLS.Worker handles the async concurrent staff notification blast
/// after consuming the SosTriggeredEvent / ComplaintStatusChangedEvent.
/// </summary>
public sealed class TriggerSosCommandHandler
    : IRequestHandler<TriggerSosCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public TriggerSosCommandHandler(
        IComplaintRepository complaintRepository,
        ICurrentPropertyContext propertyContext,
        IPublisher publisher)
    {
        _complaintRepository = complaintRepository;
        _propertyContext = propertyContext;
        _publisher = publisher;
    }

    public async Task<Result<ComplaintDto>> Handle(
        TriggerSosCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = Complaint.Create(
            title: command.Title,
            description: command.Description,
            category: "SOS_EMERGENCY",
            urgency: Urgency.SOS_EMERGENCY,
            unitId: command.UnitId,
            residentId: _propertyContext.UserId,
            propertyId: _propertyContext.PropertyId,
            permissionToEnter: command.PermissionToEnter,
            requiredSkills: []);

        var sosResult = complaint.TriggerSos();
        if (sosResult.IsFailure)
            return Result<ComplaintDto>.Failure(sosResult.Error);

        await _complaintRepository.AddAsync(complaint, cancellationToken);

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
