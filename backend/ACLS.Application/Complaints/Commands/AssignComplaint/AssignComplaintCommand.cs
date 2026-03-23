using ACLS.Application.Common.Behaviours;
using ACLS.Application.Complaints.DTOs;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.AssignComplaint;

/// <summary>
/// Command to assign a Complaint to a StaffMember.
/// Must be atomic: both Complaint.Assign() and StaffMember.MarkBusy() persist in one transaction.
/// Implements ITransactionalCommand so TransactionBehaviour wraps this handler.
/// </summary>
public sealed record AssignComplaintCommand(
    int ComplaintId,
    int StaffMemberId) : IRequest<Result<ComplaintDto>>, ITransactionalCommand;
