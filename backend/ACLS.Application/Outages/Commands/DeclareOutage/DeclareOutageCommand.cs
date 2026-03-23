using ACLS.Application.Outages.DTOs;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Outages.Commands.DeclareOutage;

/// <summary>
/// Command to declare a property-wide service outage.
/// The HTTP response returns after the outage record is persisted.
/// Mass broadcast to residents runs asynchronously in ACLS.Worker via OutageDeclaredEvent.
/// </summary>
public sealed record DeclareOutageCommand(
    string Title,
    string OutageType,
    string Description,
    DateTime StartTime,
    DateTime? EndTime) : IRequest<Result<OutageDto>>;
