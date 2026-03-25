using ACLS.Application.Complaints.DTOs;
using ACLS.SharedKernel;
using MediatR;

namespace ACLS.Application.Complaints.Commands.SubmitComplaint;

/// <summary>
/// Command to submit a new maintenance complaint.
/// Media upload to blob storage is performed in the API controller BEFORE this command is sent.
/// This command only receives URL strings resulting from the upload — never binary data.
/// </summary>
public sealed record SubmitComplaintCommand(
    string Title,
    string Description,
    string Category,
    string Urgency,
    bool PermissionToEnter,
    List<MediaUploadResult>? MediaUrls = null) : IRequest<Result<ComplaintDto>>;

/// <summary>Represents a completed blob upload: the URL and MIME type returned by IStorageService.</summary>
public sealed record MediaUploadResult(string Url, string Type);
