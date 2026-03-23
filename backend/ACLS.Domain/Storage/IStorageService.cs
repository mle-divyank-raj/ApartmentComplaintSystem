namespace ACLS.Domain.Storage;

/// <summary>
/// Blob storage service interface. Used to upload media files and return persisted blob URLs.
/// The returned URL is stored in Media.Url — binary data is never stored in MSSQL.
/// Implemented by ACLS.Infrastructure.Storage.BlobStorageService (Azure Blob or S3).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file from a stream and returns the public or SAS URL of the stored blob.
    /// Callers must store the returned URL string — not the binary content — in the database.
    /// </summary>
    /// <param name="stream">The file byte stream.</param>
    /// <param name="fileName">Original file name (used to derive blob path).</param>
    /// <param name="contentType">MIME type e.g. "image/jpeg".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full URL of the stored blob.</returns>
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct);

    /// <summary>Deletes a previously uploaded blob by URL.</summary>
    Task DeleteAsync(string blobUrl, CancellationToken ct);
}
