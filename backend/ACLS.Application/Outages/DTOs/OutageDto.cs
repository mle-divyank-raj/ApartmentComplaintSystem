using ACLS.Domain.Outages;

namespace ACLS.Application.Outages.DTOs;

/// <summary>
/// DTO returned from DeclareOutageCommandHandler and outage queries.
/// </summary>
public sealed record OutageDto(
    int OutageId,
    int PropertyId,
    int DeclaredByManagerUserId,
    string Title,
    string OutageType,
    string Description,
    DateTime StartTime,
    DateTime? EndTime,
    DateTime DeclaredAt,
    DateTime? NotificationSentAt)
{
    public static OutageDto FromDomain(Outage outage) => new(
        outage.OutageId,
        outage.PropertyId,
        outage.DeclaredByManagerUserId,
        outage.Title,
        outage.OutageType.ToString(),
        outage.Description,
        outage.StartTime,
        outage.EndTime,
        outage.DeclaredAt,
        outage.NotificationSentAt);
}
