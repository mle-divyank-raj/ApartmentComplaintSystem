using ACLS.SharedKernel;

namespace ACLS.Domain.Complaints;

/// <summary>
/// A file attachment linked to a Complaint. Stores only the blob storage URL — never binary content.
/// Binary content lives in Azure Blob Storage / AWS S3. Only the URL string is persisted in MSSQL.
/// Table: Media
/// </summary>
public sealed class Media : EntityBase
{
    public int MediaId { get; private set; }
    public int ComplaintId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public int UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    /// <summary>Private parameterless constructor for EF Core.</summary>
    private Media() { }

    /// <summary>
    /// Creates a Media record linking a blob storage URL to a Complaint.
    /// Url must be a full blob storage URL returned by IStorageService.UploadAsync.
    /// Type must be a MIME type string e.g. "image/jpeg", "image/png".
    /// Binary content must never be passed to this method.
    /// </summary>
    public static Media Create(
        int complaintId,
        string url,
        string type,
        int uploadedByUserId)
    {
        Guard.Against.NegativeOrZero(complaintId, nameof(complaintId));
        Guard.Against.NullOrWhiteSpace(url, nameof(url));
        Guard.Against.NullOrWhiteSpace(type, nameof(type));
        Guard.Against.NegativeOrZero(uploadedByUserId, nameof(uploadedByUserId));

        return new Media
        {
            ComplaintId = complaintId,
            Url = url,
            Type = type,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow
        };
    }
}
