using ACLS.Domain.Complaints;

namespace ACLS.Application.Complaints.DTOs;

/// <summary>
/// Data Transfer Object for a Media attachment. Contains only the URL string — never binary content.
/// </summary>
public sealed record MediaDto(
    int MediaId,
    string Url,
    string Type,
    int UploadedByUserId,
    DateTime UploadedAt)
{
    public static MediaDto FromDomain(Media media) => new(
        media.MediaId,
        media.Url,
        media.Type,
        media.UploadedByUserId,
        media.UploadedAt);
}
