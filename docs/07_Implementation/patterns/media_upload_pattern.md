# Media Upload Pattern

**Document:** `docs/07_Implementation/patterns/media_upload_pattern.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> Binary files are never stored in MSSQL. This rule has no exceptions. Every media upload follows the two-step pattern described in this document: upload to blob storage first, persist only the URL to the database second. Any implementation that stores `byte[]`, `varbinary`, or base64-encoded content in a database column violates this pattern and must be corrected.

---

## 1. Why This Pattern

Storing binary files in a relational database causes three problems this pattern avoids:

1. **Unbounded row size** — A single complaint with three 5MB images would store 15MB in one row, destroying query performance across the entire `Complaints` table.
2. **Backup size explosion** — Database backups grow proportionally to media volume, making point-in-time restore impractical.
3. **Streaming inefficiency** — Blob storage services (Azure Blob, S3) are purpose-built for streaming large binary content efficiently to mobile clients. A SQL Server round-trip is not.

The pattern stores only a URL string (≤2000 chars) in MSSQL. The binary content lives in Azure Blob Storage and is served directly from there.

---

## 2. The Two-Step Sequence

```
Client submits multipart/form-data
    │
    │  Fields: title, description, category, urgency, permissionToEnter
    │  Files: [photo1.jpg, photo2.jpg]
    ▼
ComplaintsController.SubmitComplaint()
    │
    │  Step 1: Upload each file to blob storage
    │  ┌──────────────────────────────────────────────────────────┐
    │  │  foreach file in request.Files:                          │
    │  │    url = await IStorageService.UploadAsync(              │
    │  │              fileStream, fileName, contentType)          │
    │  │    ← returns permanent blob URL string                  │
    │  └──────────────────────────────────────────────────────────┘
    │
    │  Step 2: Build command with URLs (not file streams)
    │  ┌──────────────────────────────────────────────────────────┐
    │  │  var mediaUrls = uploadedFiles                           │
    │  │      .Select(f => new MediaUploadResult(f.Url, f.Type)) │
    │  │      .ToList();                                          │
    │  │                                                          │
    │  │  var command = new SubmitComplaintCommand(               │
    │  │      title, description, category, urgency,              │
    │  │      permissionToEnter, mediaUrls);                      │
    │  └──────────────────────────────────────────────────────────┘
    ▼
SubmitComplaintCommandHandler
    │
    │  Creates Complaint entity
    │  Creates Media entity per URL:
    │    new Media(url: f.Url, type: f.Type, complaintId: complaint.ComplaintId)
    │  Persists Complaint + Media to MSSQL
    │  (URL strings only — zero binary content in the database)
    ▼
Response: ComplaintDto with media[].url populated
```

**The file streams never leave the controller.** The command carries URL strings, not streams or byte arrays. The Application and Domain layers never see binary content.

---

## 3. IStorageService Interface

**Location:** `ACLS.Domain/Storage/IStorageService.cs`

```csharp
namespace ACLS.Domain.Storage;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to blob storage and returns the permanent URL.
    /// The caller is responsible for closing the stream after this call.
    /// </summary>
    /// <param name="stream">The file content stream (read-only).</param>
    /// <param name="fileName">Original filename — used to derive blob name and extension.</param>
    /// <param name="contentType">MIME type e.g. "image/jpeg".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Permanent public URL to the uploaded blob.</returns>
    Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct);
}
```

**Implementation location:** `ACLS.Infrastructure/Storage/AzureBlobStorageService.cs`

The implementation is in `ACLS.Infrastructure`. The controller and command handler depend only on the `IStorageService` interface from `ACLS.Domain`.

---

## 4. Azure Blob Storage Implementation

```csharp
// ACLS.Infrastructure/Storage/AzureBlobStorageService.cs
namespace ACLS.Infrastructure.Storage;

public sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<StorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerName = options.Value.MediaContainerName;  // e.g. "acls-media"
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        // Generate a unique blob name to prevent collisions
        var blobName = GenerateBlobName(fileName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(stream, uploadOptions, ct);

        _logger.LogInformation(
            "Uploaded media blob {BlobName} ({ContentType})", blobName, contentType);

        // Return the full URL — this is what gets stored in Media.Url
        return blobClient.Uri.ToString();
    }

    private static string GenerateBlobName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniqueId = Guid.NewGuid().ToString("N");  // 32 hex chars, no hyphens
        return $"media/{timestamp}/{uniqueId}{extension}";
        // Example: media/20260201/a3f7d9b2c1e4f8a0b6d3e9c7f2a1b4d5.jpg
    }
}
```

**Local development:** Replace `AzureBlobStorageService` with `AzuriteStorageService` (same code, different connection string pointing to the Azurite emulator at `http://127.0.0.1:10000/devstoreaccount1`). Configured via `appsettings.Development.json`.

---

## 5. Controller Implementation

The upload step happens in the controller — not in the command handler. The controller is the boundary between the HTTP world (multipart streams) and the application world (URL strings).

```csharp
// ACLS.Api/Controllers/ComplaintsController.cs
[HttpPost]
[Authorize(Roles = "Resident")]
[Consumes("multipart/form-data")]
[ProducesResponseType(typeof(ComplaintDto), StatusCodes.Status201Created)]
public async Task<IActionResult> SubmitComplaint(
    [FromForm] SubmitComplaintFormRequest request,
    CancellationToken cancellationToken)
{
    // Step 1: Validate file count before uploading
    if (request.MediaFiles?.Count > ComplaintConstants.MaxMediaAttachments)
    {
        return BadRequest(new ValidationProblemDetails
        {
            Errors = { ["mediaFiles"] = [$"Maximum {ComplaintConstants.MaxMediaAttachments} files allowed."] }
        });
    }

    // Step 2: Upload each file to blob storage
    var mediaUploadResults = new List<MediaUploadResult>();

    if (request.MediaFiles is { Count: > 0 })
    {
        foreach (var file in request.MediaFiles)
        {
            ValidateMediaFile(file);  // checks MIME type and size

            await using var stream = file.OpenReadStream();
            var url = await _storageService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);

            mediaUploadResults.Add(new MediaUploadResult(url, file.ContentType));
        }
    }

    // Step 3: Build and send the command (URLs only — no streams)
    var command = new SubmitComplaintCommand(
        Title: request.Title,
        Description: request.Description,
        Category: request.Category,
        Urgency: request.Urgency,
        PermissionToEnter: request.PermissionToEnter,
        MediaFiles: mediaUploadResults);   // ← URL strings, not streams

    var result = await Mediator.Send(command, cancellationToken);

    if (result.IsFailure)
        return MapError(result.Error);

    return CreatedAtAction(
        nameof(GetComplaint),
        new { complaintId = result.Value.ComplaintId },
        result.Value);
}

private static void ValidateMediaFile(IFormFile file)
{
    var allowedTypes = new[] { "image/jpeg", "image/png" };

    if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
        throw new ValidationException(
            $"File type '{file.ContentType}' is not allowed. Only JPEG and PNG are accepted.");

    const long maxFileSizeBytes = 5 * 1024 * 1024;  // 5MB
    if (file.Length > maxFileSizeBytes)
        throw new ValidationException(
            $"File '{file.FileName}' exceeds the maximum size of 5MB.");
}
```

**`_storageService`** is injected via constructor: `IStorageService _storageService`. This is the only place in `ACLS.Api` where `IStorageService` is used directly. The command handler never sees it.

---

## 6. Command Handler Implementation

The command handler receives URL strings — never file streams. It creates `Media` entities with the URLs and persists them.

```csharp
// ACLS.Application/Complaints/Commands/SubmitComplaint/SubmitComplaintCommandHandler.cs
internal sealed class SubmitComplaintCommandHandler
    : IRequestHandler<SubmitComplaintCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public async Task<Result<ComplaintDto>> Handle(
        SubmitComplaintCommand command,
        CancellationToken ct)
    {
        // Create the complaint entity
        var complaint = Complaint.Create(
            title: command.Title,
            description: command.Description,
            category: command.Category,
            urgency: command.Urgency,
            unitId: _propertyContext.UnitId,
            residentId: _propertyContext.UserId,
            propertyId: _propertyContext.PropertyId,
            permissionToEnter: command.PermissionToEnter);

        // Create Media entities from URL strings — no binary content
        foreach (var mediaFile in command.MediaFiles)
        {
            var media = Media.Create(
                url: mediaFile.Url,          // ← URL string from blob storage
                type: mediaFile.ContentType, // ← MIME type string
                complaintId: complaint.ComplaintId,
                uploadedByUserId: _propertyContext.UserId);

            complaint.AddMedia(media);
        }

        await _complaintRepository.AddAsync(complaint, ct);

        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, ct);

        complaint.ClearDomainEvents();

        return Result.Success(ComplaintDto.FromDomain(complaint));
    }
}
```

The command definition makes the URL-only contract explicit:

```csharp
// SubmitComplaintCommand.cs
public sealed record SubmitComplaintCommand(
    string Title,
    string Description,
    string Category,
    Urgency Urgency,
    bool PermissionToEnter,
    IReadOnlyList<MediaUploadResult> MediaFiles)  // ← URL strings, never streams
    : IRequest<Result<ComplaintDto>>;

// MediaUploadResult — simple value holder
public sealed record MediaUploadResult(string Url, string ContentType);
```

---

## 7. The Same Pattern for Completion Photos (Resolve Complaint)

When a staff member resolves a complaint with completion photos, the same two-step pattern applies:

```csharp
// ResolveComplaintController action
[HttpPost("{complaintId:int}/resolve")]
[Authorize(Roles = "MaintenanceStaff")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> ResolveComplaint(
    [FromRoute] int complaintId,
    [FromForm] ResolveComplaintFormRequest request,
    CancellationToken cancellationToken)
{
    // Step 1: Upload completion photos to blob storage
    var completionPhotoUrls = new List<MediaUploadResult>();

    if (request.CompletionPhotos is { Count: > 0 })
    {
        foreach (var photo in request.CompletionPhotos)
        {
            ValidateMediaFile(photo);
            await using var stream = photo.OpenReadStream();
            var url = await _storageService.UploadAsync(
                stream, photo.FileName, photo.ContentType, cancellationToken);
            completionPhotoUrls.Add(new MediaUploadResult(url, photo.ContentType));
        }
    }

    // Step 2: Send command with URLs
    var command = new ResolveComplaintCommand(
        ComplaintId: complaintId,
        ResolutionNotes: request.ResolutionNotes,
        CompletionPhotos: completionPhotoUrls);

    var result = await Mediator.Send(command, cancellationToken);
    return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
}
```

The handler for `ResolveComplaintCommand` follows the same pattern as `SubmitComplaintCommandHandler` — creates `Media` entities from URL strings.

---

## 8. Media Entity

```csharp
// ACLS.Domain/Complaints/Media.cs
namespace ACLS.Domain.Complaints;

public sealed class Media : EntityBase
{
    public int MediaId { get; private set; }
    public int ComplaintId { get; private set; }
    public string Url { get; private set; }          // blob URL string — never null
    public string Type { get; private set; }         // MIME type string — never null
    public int UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Media() { }  // EF Core

    public static Media Create(
        string url,
        string type,
        int complaintId,
        int uploadedByUserId)
    {
        Guard.Against.NullOrWhiteSpace(url, nameof(url));
        Guard.Against.NullOrWhiteSpace(type, nameof(type));

        return new Media
        {
            Url = url,
            Type = type,
            ComplaintId = complaintId,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow
        };
    }
}
```

There is no `byte[]`, `Stream`, `BinaryData`, or `varbinary` field anywhere on this entity. The `Url` property is a plain `string`. This is the complete contract.

---

## 9. Local Development with Azurite

In local development, `IStorageService` is implemented by `AzureStorageService` pointing at the Azurite emulator:

```json
// appsettings.Development.json (gitignored)
{
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "MediaContainerName": "acls-media-dev"
  }
}
```

Azurite runs on port 10000 via Docker Compose. Blob URLs in local development look like:
```
http://127.0.0.1:10000/devstoreaccount1/acls-media-dev/media/20260201/abc123.jpg
```

These URLs are stored in the `Media.Url` column during local development and returned in API responses. They are not accessible from Android/iOS emulators unless the emulator is configured to reach the host machine. During local testing use the web client or Postman.

---

## 10. Checklist — Before Committing Any Media Handling Code

- [ ] Does the controller upload files to `IStorageService` before building the command?
- [ ] Does the command carry `IReadOnlyList<MediaUploadResult>` (URL strings), not `IFormFile` or `Stream`?
- [ ] Does the command handler create `Media` entities from URL strings only?
- [ ] Is there any `byte[]`, `varbinary`, `Stream`, or `BinaryData` in any entity, DTO, or database column? If yes — remove it.
- [ ] Does the `Media` entity's `Url` property map to an `nvarchar(2000)` column in the EF Core configuration?
- [ ] Is the blob upload before the database write in the request flow? (If blob upload fails, nothing is written to the database.)

---

*End of Media Upload Pattern v1.0*
