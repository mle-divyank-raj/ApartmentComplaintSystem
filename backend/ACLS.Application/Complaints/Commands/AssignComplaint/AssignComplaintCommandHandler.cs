using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.AssignComplaint;

/// <summary>
/// Handles Complaint assignment to a StaffMember.
/// Both the Complaint status change and the StaffMember availability change are committed atomically
/// via TransactionBehaviour (triggered because AssignComplaintCommand implements ITransactionalCommand).
///
/// Multi-tenancy: both GetByIdAsync calls include PropertyId so cross-property access returns null → 404.
/// Domain events are published AFTER the handler returns (post-transaction commit in the behaviour).
/// </summary>
public sealed class AssignComplaintCommandHandler
    : IRequestHandler<AssignComplaintCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public AssignComplaintCommandHandler(
        IComplaintRepository complaintRepository,
        IStaffRepository staffRepository,
        ICurrentPropertyContext propertyContext,
        IPublisher publisher)
    {
        _complaintRepository = complaintRepository;
        _staffRepository = staffRepository;
        _propertyContext = propertyContext;
        _publisher = publisher;
    }

    public async Task<Result<ComplaintDto>> Handle(
        AssignComplaintCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<ComplaintDto>.Failure(
                ComplaintErrors.NotFound(command.ComplaintId));

        var staff = await _staffRepository.GetByIdAsync(
            command.StaffMemberId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (staff is null)
            return Result<ComplaintDto>.Failure(
                new Error("StaffMember.NotFound",
                    $"Staff member with ID {command.StaffMemberId} was not found."));

        var assignResult = complaint.Assign(command.StaffMemberId);
        if (assignResult.IsFailure)
            return Result<ComplaintDto>.Failure(assignResult.Error);

        staff.MarkBusy();

        await _complaintRepository.UpdateAsync(complaint, cancellationToken);
        await _staffRepository.UpdateAsync(staff, cancellationToken);

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        foreach (var domainEvent in staff.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();
        staff.ClearDomainEvents();

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
