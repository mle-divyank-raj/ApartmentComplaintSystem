using ACLS.Application.Common.Behaviours;
using ACLS.Application.Complaints.DTOs;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.ResolveComplaint;

/// <summary>
/// Command to mark a Complaint as resolved by the assigned StaffMember.
/// Complaint must be IN_PROGRESS. Caller must provide optional completion photo URLs.
/// Atomic: Complaint.Resolve() + StaffMember.MarkAvailable() in one transaction.
/// </summary>
public sealed record ResolveComplaintCommand(
    int ComplaintId,
    List<MediaUploadResult>? CompletionPhotoUrls = null) : IRequest<Result<ComplaintDto>>, ITransactionalCommand;

/// <summary>
/// A completed blob upload (URL + MIME type) for a completion photo.
/// Matches the same type used in SubmitComplaintCommand for consistency.
/// </summary>
public sealed record MediaUploadResult(string Url, string Type);
