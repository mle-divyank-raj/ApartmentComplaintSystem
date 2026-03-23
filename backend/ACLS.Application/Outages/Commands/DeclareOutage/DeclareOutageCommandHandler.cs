using ACLS.Application.Common.Interfaces;
using ACLS.Application.Outages.DTOs;
using ACLS.Domain.Outages;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Outages.Commands.DeclareOutage;

/// <summary>
/// Handles outage declaration.
/// Creates the Outage aggregate, persists it, then publishes OutageDeclaredEvent.
/// ACLS.Worker handles the async mass notification fan-out to all residents.
/// The NotificationService is NEVER called inline from this handler.
/// </summary>
public sealed class DeclareOutageCommandHandler
    : IRequestHandler<DeclareOutageCommand, Result<OutageDto>>
{
    private readonly IOutageRepository _outageRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public DeclareOutageCommandHandler(
        IOutageRepository outageRepository,
        ICurrentPropertyContext propertyContext,
        IPublisher publisher)
    {
        _outageRepository = outageRepository;
        _propertyContext = propertyContext;
        _publisher = publisher;
    }

    public async Task<Result<OutageDto>> Handle(
        DeclareOutageCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OutageType>(command.OutageType, ignoreCase: true, out var outageType))
            return Result<OutageDto>.Failure(
                new Error("Outage.InvalidType", $"'{command.OutageType}' is not a valid outage type."));

        var outage = Outage.Declare(
            propertyId: _propertyContext.PropertyId,
            declaredByManagerUserId: _propertyContext.UserId,
            title: command.Title,
            outageType: outageType,
            description: command.Description,
            startTime: command.StartTime,
            endTime: command.EndTime);

        await _outageRepository.AddAsync(outage, cancellationToken);

        foreach (var domainEvent in outage.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        outage.ClearDomainEvents();

        return Result<OutageDto>.Success(OutageDto.FromDomain(outage));
    }
}
