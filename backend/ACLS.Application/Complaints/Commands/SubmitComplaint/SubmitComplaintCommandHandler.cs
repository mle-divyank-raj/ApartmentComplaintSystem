using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.DTOs;
using ACLS.Domain.Complaints;
using ACLS.Domain.Residents;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.SubmitComplaint;

/// <summary>
/// Handles complaint submission.
/// Creates a new Complaint aggregate, persists it, then persists Media records for each uploaded URL.
/// PropertyId and ResidentId are sourced from ICurrentPropertyContext (JWT claims) — never from the command.
/// UnitId is resolved from the Resident record via IResidentRepository using the authenticated UserId.
/// RequiredSkills are derived from Category (simple word tokenisation for Phase 2 scope).
/// </summary>
public sealed class SubmitComplaintCommandHandler
    : IRequestHandler<SubmitComplaintCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IResidentRepository _residentRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public SubmitComplaintCommandHandler(
        IComplaintRepository complaintRepository,
        IResidentRepository residentRepository,
        ICurrentPropertyContext propertyContext,
        IPublisher publisher)
    {
        _complaintRepository = complaintRepository;
        _residentRepository = residentRepository;
        _propertyContext = propertyContext;
        _publisher = publisher;
    }

    public async Task<Result<ComplaintDto>> Handle(
        SubmitComplaintCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Urgency>(command.Urgency, ignoreCase: true, out var urgency))
            return Result<ComplaintDto>.Failure(
                new Error("Complaint.InvalidUrgency", $"'{command.Urgency}' is not a valid urgency level."));

        var resident = await _residentRepository.GetByUserIdAsync(
            _propertyContext.UserId, _propertyContext.PropertyId, cancellationToken);

        if (resident is null)
            return Result<ComplaintDto>.Failure(
                new Error("Resident.NotFound", "No resident profile found for the authenticated user."));

        var requiredSkills = new List<string> { command.Category };

        var complaint = Complaint.Create(
            title: command.Title,
            description: command.Description,
            category: command.Category,
            urgency: urgency,
            unitId: resident.UnitId,
            residentId: resident.ResidentId,
            propertyId: _propertyContext.PropertyId,
            permissionToEnter: command.PermissionToEnter,
            requiredSkills: requiredSkills);

        await _complaintRepository.AddAsync(complaint, cancellationToken);

        if (command.MediaUrls is { Count: > 0 })
        {
            foreach (var upload in command.MediaUrls)
            {
                var media = Media.Create(
                    complaintId: complaint.ComplaintId,
                    url: upload.Url,
                    type: upload.Type,
                    uploadedByUserId: _propertyContext.UserId);

                await _complaintRepository.AddMediaAsync(media, cancellationToken);
            }
        }

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();

        return Result<ComplaintDto>.Success(ComplaintDto.FromDomain(complaint));
    }
}
