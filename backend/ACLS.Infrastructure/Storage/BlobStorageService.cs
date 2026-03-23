using ACLS.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACLS.Infrastructure.Storage;

/// <summary>
/// Stub implementation of IStorageService. Generates a placeholder URL for development.
/// Replace with Azure Blob Storage SDK (Azure.Storage.Blobs) for production.
/// Binary data is NEVER stored in the database — only the returned URL string is persisted.
/// </summary>
public sealed class BlobStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        // Stub: generate a deterministic placeholder URL.
        // In production, upload to Azure Blob Storage and return the real SAS/CDN URL.
        var blobName = $"{Guid.NewGuid():N}_{fileName}";
        var containerBase = _configuration["BlobStorage:ContainerUrl"]
            ?? "https://acls-dev-storage.blob.core.windows.net/media";

        var url = $"{containerBase.TrimEnd('/')}/{blobName}";

        _logger.LogDebug("Stub upload: {FileName} → {Url}", fileName, url);

        return Task.FromResult(url);
    }

    public Task DeleteAsync(string blobUrl, CancellationToken ct)
    {
        // Stub: no-op delete. In production, remove the blob from Azure Blob Storage.
        _logger.LogDebug("Stub delete: {BlobUrl}", blobUrl);
        return Task.CompletedTask;
    }
}
