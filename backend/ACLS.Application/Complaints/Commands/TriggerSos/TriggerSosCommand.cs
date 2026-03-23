using ACLS.Application.Complaints.DTOs;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.TriggerSos;

/// <summary>
/// Command to submit an SOS emergency complaint.
/// Creates a new Complaint with Urgency=SOS_EMERGENCY and immediately sets status to ASSIGNED
/// via Complaint.TriggerSos(). Notifying all on-call staff is handled asynchronously by ACLS.Worker.
/// </summary>
public sealed record TriggerSosCommand(
    string Title,
    string Description,
    int UnitId,
    bool PermissionToEnter) : IRequest<Result<ComplaintDto>>;
