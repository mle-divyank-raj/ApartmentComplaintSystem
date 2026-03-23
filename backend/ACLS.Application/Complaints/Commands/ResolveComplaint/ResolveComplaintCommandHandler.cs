using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Staff;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.ResolveComplaint;

/// <summary>
/// Handles Complaint resolution.
/// Complaint must be IN_PROGRESS. Atomically transitions Complaint → RESOLVED and Staff → AVAILABLE.
/// Adds any completion photos as Media records.
/// TAT calculation runs asynchronously in ACLS.Worker via ComplaintResolvedEvent.
/// </summary>
public sealed class ResolveComplaintCommandHandler
    : IRequestHandler<ResolveComplaintCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public ResolveComplaintCommandHandler(
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
        ResolveComplaintCommand command,
        CancellationToken cancellationToken)
    {
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result<ComplaintDto>.Failure(
                ComplaintErrors.NotFound(command.ComplaintId));

        var resolveResult = complaint.Resolve();
        if (resolveResult.IsFailure)
            return Result<ComplaintDto>.Failure(resolveResult.Error);

        var staff = await _staffRepository.GetByIdAsync(
            complaint.AssignedStaffMemberId!.Value,
            _propertyContext.PropertyId,
            cancellationToken);

        if (staff is null)
            return Result<ComplaintDto>.Failure(
                new Error("StaffMember.NotFound",
                    $"Assigned staff member with ID {complaint.AssignedStaffMemberId} was not found."));

        staff.MarkAvailable();

        await _complaintRepository.UpdateAsync(complaint, cancellationToken);
        await _staffRepository.UpdateAsync(staff, cancellationToken);

        if (command.CompletionPhotoUrls is { Count: > 0 })
        {
            foreach (var photo in command.CompletionPhotoUrls)
            {
                var media = Media.Create(
                    complaintId: complaint.ComplaintId,
                    url: photo.Url,
                    type: photo.Type,
                    uploadedByUserId: _propertyContext.UserId);

                await _complaintRepository.AddMediaAsync(media, cancellationToken);
            }
        }

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        foreach (var domainEvent in staff.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();
        staff.ClearDomainEvents();

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
