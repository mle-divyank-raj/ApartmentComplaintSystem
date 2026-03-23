# Coding Standards

**Document:** `docs/07_Implementation/coding_standards.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document defines the mandatory coding conventions for every file generated in this project across all four platforms: C# backend, TypeScript/React web, Kotlin Android, and Swift iOS. These conventions are not preferences — they are enforced at pull request review. AI-generated code that violates these standards must be corrected before it is considered complete output.

---

## 1. General Principles (All Platforms)

These principles apply to every line of code generated in this project, regardless of platform.

**One class, one responsibility.** No class, struct, or component may serve more than one purpose. A command handler handles one command. A repository implements one bounded context's persistence. A Composable renders one logical UI concern. If a class is doing two things, split it.

**Explicit over implicit.** Always state intent clearly. Avoid clever shortcuts that obscure what the code is doing. A junior developer reading the code should be able to understand what it does without consulting a senior developer.

**No magic strings or magic numbers.** All string constants (route paths, JWT claim names, configuration keys, error codes, enum string values) and all numeric constants (timeout values, score weights, max attachment counts) must be defined as named constants or typed enum values. Never inline them.

```csharp
// WRONG
if (user.Role == "MaintenanceStaff") { ... }
var maxAttachments = 3;

// CORRECT
if (user.Role == Role.MaintenanceStaff) { ... }
// In a constants file:
public static class ComplaintConstants
{
    public const int MaxMediaAttachments = 3;
    public const int InvitationTokenExpiryHours = 72;
}
```

**No dead code.** Do not generate commented-out code blocks, unused variables, unused parameters, or unused imports. If something is not needed now, do not generate it. If something is stubbed for future implementation, use `throw new NotImplementedException("Reason: [explanation]")` — not a comment.

**Fail loudly, not silently.** Do not swallow exceptions. Do not return `null` where a meaningful error can be returned. Use `Result<T>` on the backend for operation outcomes. Throw explicitly when a precondition is violated.

---

## 2. C# Backend Standards (`ACLS.*` projects)

### 2.1 Language and Framework Version

- Target framework: `.NET 8`
- C# language version: `12` (default for .NET 8)
- Nullable reference types: **enabled** in all projects (`<Nullable>enable</Nullable>` in `.csproj`)
- Implicit usings: **enabled** (`<ImplicitUsings>enable</ImplicitUsings>`)

### 2.2 Naming

| Concept | Convention | Example |
|---|---|---|
| Namespaces | Match project + folder path | `ACLS.Domain.Complaints` |
| Classes | PascalCase noun or noun phrase | `ComplaintRepository`, `StaffMember` |
| Interfaces | `I` prefix + PascalCase | `IComplaintRepository`, `IDispatchService` |
| Enums | PascalCase type, SCREAMING_SNAKE values | `TicketStatus.IN_PROGRESS` |
| Methods | PascalCase verb phrase | `AssignComplaintAsync`, `FindOptimalStaff` |
| Properties | PascalCase noun | `PropertyId`, `CreatedAt`, `MatchScore` |
| Private fields | `_camelCase` with underscore prefix | `_mediator`, `_context`, `_storageService` |
| Local variables | `camelCase` | `complaintId`, `staffScore`, `existingComplaint` |
| Constants | `PascalCase` in a static class | `ComplaintConstants.MaxMediaAttachments` |
| Async methods | Always suffix with `Async` | `GetComplaintByIdAsync`, `UploadAsync` |
| Parameters | `camelCase` | `complaintId`, `cancellationToken` |
| Generic type parameters | Single uppercase letter or `T` prefix | `T`, `TResult`, `TEntity` |

### 2.3 File Organisation

One class per file. File name matches class name exactly.

```
// File: AssignComplaintCommandHandler.cs
namespace ACLS.Application.Complaints.Commands.AssignComplaint;

public sealed class AssignComplaintCommandHandler : IRequestHandler<AssignComplaintCommand, Result<ComplaintDto>>
{
    // ...
}
```

Folder structure within a project mirrors the namespace:

```
ACLS.Application/
└── Complaints/
    └── Commands/
        └── AssignComplaint/
            ├── AssignComplaintCommand.cs
            ├── AssignComplaintCommandHandler.cs
            └── AssignComplaintValidator.cs
```

### 2.4 Async/Await

All I/O-bound operations (database queries, file uploads, HTTP calls, notification dispatch) must be `async`. All async methods must:
- Return `Task` or `Task<T>` — never `void` (except event handlers)
- Accept a `CancellationToken` parameter named `cancellationToken` or `ct`
- Propagate the `CancellationToken` to every awaited call

```csharp
// CORRECT
public async Task<Result<ComplaintDto>> Handle(
    GetComplaintByIdQuery query,
    CancellationToken cancellationToken)
{
    var complaint = await _repository.GetByIdAsync(query.ComplaintId, cancellationToken);
    if (complaint is null)
        return Result.Failure<ComplaintDto>(ComplaintErrors.NotFound(query.ComplaintId));

    return Result.Success(_mapper.Map<ComplaintDto>(complaint));
}

// WRONG — blocks thread, no cancellation support
public ComplaintDto Handle(GetComplaintByIdQuery query)
{
    var complaint = _repository.GetById(query.ComplaintId);
    return _mapper.Map<ComplaintDto>(complaint);
}
```

### 2.5 Result Pattern

All Application layer command and query handlers must return `Result<T>` (or `Result` for void operations) instead of throwing exceptions for expected failure conditions. Use the `Result<T>` type defined in `ACLS.SharedKernel`.

```csharp
// Failure cases return Result.Failure — they do not throw
if (complaint is null)
    return Result.Failure<ComplaintDto>(ComplaintErrors.NotFound(complaintId));

if (complaint.PropertyId != _currentPropertyContext.PropertyId)
    return Result.Failure<ComplaintDto>(ComplaintErrors.AccessDenied());

// Success returns Result.Success
return Result.Success(new ComplaintDto(complaint));
```

Exceptions are reserved for truly unexpected failures (infrastructure outages, unrecoverable states). Business rule violations and "not found" scenarios always use `Result.Failure`.

The `ACLS.Api` controller maps `Result` to HTTP responses:

```csharp
return result.IsSuccess
    ? Ok(result.Value)
    : result.Error.Code switch
    {
        "Complaint.NotFound" => NotFound(result.Error),
        "Complaint.AccessDenied" => Forbid(),
        _ => Problem(result.Error.Message)
    };
```

### 2.6 Entity Design

All domain entities inherit from `EntityBase` defined in `ACLS.SharedKernel`:

```csharp
// In ACLS.SharedKernel
public abstract class EntityBase
{
    public int Id { get; protected set; }
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

Entity rules:
- Constructors are `private` or `protected`. Use static factory methods for creation.
- Properties have `private set` or `init` — never `public set` on domain entities.
- State changes happen through explicit methods that enforce invariants and raise domain events.
- No public parameterless constructor (EF Core uses a private one via reflection).

```csharp
// CORRECT
public sealed class Complaint : EntityBase
{
    public int ComplaintId { get; private set; }
    public TicketStatus Status { get; private set; }
    public int PropertyId { get; private set; }

    private Complaint() { } // EF Core

    public static Complaint Create(
        string title,
        string description,
        Urgency urgency,
        int unitId,
        int residentId,
        int propertyId,
        bool permissionToEnter)
    {
        var complaint = new Complaint
        {
            Title = title,
            Description = description,
            Urgency = urgency,
            Status = TicketStatus.OPEN,
            UnitId = unitId,
            ResidentId = residentId,
            PropertyId = propertyId,
            PermissionToEnter = permissionToEnter,
            CreatedAt = DateTime.UtcNow
        };
        complaint.RaiseDomainEvent(new ComplaintSubmittedEvent(complaint.ComplaintId, propertyId));
        return complaint;
    }

    public Result Assign(int staffMemberId)
    {
        if (Status != TicketStatus.OPEN && Status != TicketStatus.ASSIGNED)
            return Result.Failure(ComplaintErrors.InvalidStatusTransition(Status, TicketStatus.ASSIGNED));

        Status = TicketStatus.ASSIGNED;
        AssignedStaffMemberId = staffMemberId;
        RaiseDomainEvent(new ComplaintAssignedEvent(ComplaintId, staffMemberId, PropertyId));
        return Result.Success();
    }
}
```

### 2.7 Dependency Injection

All services are registered in `Program.cs` (or extension methods called from `Program.cs`) using the appropriate lifetime:

| Lifetime | Use for |
|---|---|
| `Singleton` | Stateless services with no per-request state (e.g. `IDispatchService` if stateless) |
| `Scoped` | Per-request services (repositories, `ICurrentPropertyContext`, DbContext) |
| `Transient` | Lightweight stateless utilities |

Registration extension method pattern:

```csharp
// In ACLS.Infrastructure/DependencyInjection.cs
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IStorageService, AzureBlobStorageService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDispatchService, DispatchService>();
        return services;
    }
}
```

Never use `new` to instantiate a service that has dependencies. Never use the service locator pattern (resolving from `IServiceProvider` inside a class). Constructor injection only.

### 2.8 EF Core and Repository Conventions

DbContext is `AclsDbContext` in `ACLS.Persistence`.

Repository interface naming: `I<Entity>Repository` in `ACLS.Domain`.  
Repository implementation naming: `<Entity>Repository` in `ACLS.Persistence`.

Every repository method that queries property-scoped data must accept and apply `propertyId`:

```csharp
// Interface in ACLS.Domain
public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(int complaintId, int propertyId, CancellationToken ct);
    Task<IReadOnlyList<Complaint>> GetAllAsync(int propertyId, CancellationToken ct);
    Task AddAsync(Complaint complaint, CancellationToken ct);
    Task UpdateAsync(Complaint complaint, CancellationToken ct);
}

// Implementation in ACLS.Persistence
public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly AclsDbContext _context;

    public ComplaintRepository(AclsDbContext context) => _context = context;

    public async Task<Complaint?> GetByIdAsync(int complaintId, int propertyId, CancellationToken ct)
        => await _context.Complaints
            .Where(c => c.ComplaintId == complaintId && c.PropertyId == propertyId)
            .FirstOrDefaultAsync(ct);
}
```

**The `propertyId` filter is mandatory in every query. A query without it is a bug.**

No raw SQL (`FromSqlRaw`, `ExecuteSqlRaw`) unless explicitly approved and documented. Use LINQ only.

### 2.9 Controller Conventions

Controllers inherit from `ApiControllerBase` (a custom base class in `ACLS.Api` that applies `[ApiController]`, `[Authorize]`, and the route prefix):

```csharp
// In ACLS.Api/Controllers/ApiControllerBase.cs
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IMediator Mediator;
    protected ApiControllerBase(IMediator mediator) => Mediator = mediator;
}
```

Controller action conventions:
- One action per HTTP verb per resource operation.
- Always `async Task<IActionResult>`.
- Always accept `CancellationToken cancellationToken` as the last parameter (ASP.NET Core binds it automatically from the request abort token).
- Route paths use `kebab-case` for multi-word segments: `/api/v1/complaints/{id}/work-notes`.
- `[FromBody]` for POST/PUT/PATCH request bodies. `[FromRoute]` for IDs in the path. `[FromQuery]` for filter/search parameters.

```csharp
[HttpPost("{complaintId:int}/assign")]
[Authorize(Roles = "Manager")]
[ProducesResponseType(typeof(ComplaintDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> AssignComplaint(
    [FromRoute] int complaintId,
    [FromBody] AssignComplaintRequest request,
    CancellationToken cancellationToken)
{
    var command = new AssignComplaintCommand(complaintId, request.StaffMemberId);
    var result = await Mediator.Send(command, cancellationToken);
    return result.IsSuccess ? Ok(result.Value) : MapError(result.Error);
}
```

### 2.10 Validation

All command inputs are validated using FluentValidation. Validators live alongside their command in the same folder. MediatR pipeline behaviour triggers validation before the handler runs.

```csharp
// In ACLS.Application/Complaints/Commands/SubmitComplaint/SubmitComplaintValidator.cs
public sealed class SubmitComplaintValidator : AbstractValidator<SubmitComplaintCommand>
{
    public SubmitComplaintValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Urgency)
            .IsInEnum().WithMessage("Invalid urgency value.");

        RuleFor(x => x.MediaFiles)
            .Must(files => files == null || files.Count <= ComplaintConstants.MaxMediaAttachments)
            .WithMessage($"A maximum of {ComplaintConstants.MaxMediaAttachments} media files may be uploaded per complaint.");
    }
}
```

Validation is a cross-cutting concern. Never write manual `if (string.IsNullOrEmpty(...))` validation inside a command handler. That belongs in the validator.

### 2.11 Error Constants

All error definitions live in static error classes per bounded context:

```csharp
// In ACLS.Domain/Complaints/ComplaintErrors.cs
public static class ComplaintErrors
{
    public static Error NotFound(int complaintId) =>
        new("Complaint.NotFound", $"Complaint with ID {complaintId} was not found.");

    public static Error AccessDenied() =>
        new("Complaint.AccessDenied", "You do not have access to this complaint.");

    public static Error InvalidStatusTransition(TicketStatus from, TicketStatus to) =>
        new("Complaint.InvalidStatusTransition",
            $"Cannot transition complaint from {from} to {to}.");

    public static Error MaxMediaAttachmentsExceeded() =>
        new("Complaint.MaxMediaAttachmentsExceeded",
            $"A complaint may not have more than {ComplaintConstants.MaxMediaAttachments} media attachments.");
}
```

### 2.12 DateTime Conventions

- All `DateTime` values stored in the database and returned in API responses are **UTC**.
- Use `DateTime.UtcNow` — never `DateTime.Now`.
- Property names for timestamps: `CreatedAt`, `UpdatedAt`, `ResolvedAt`, `OccurredAt`, `StartTime`, `EndTime`.
- Database column names: `created_at`, `updated_at`, `resolved_at`, `occurred_at`, `start_time`, `end_time`.
- API JSON field names: `createdAt`, `updatedAt`, `resolvedAt` (camelCase, ISO 8601 format).

### 2.13 NUnit Test Conventions

Test class naming: `<ClassUnderTest>Tests`  
Test method naming: `<MethodUnderTest>_<Scenario>_<ExpectedOutcome>`

```csharp
// File: DispatchServiceTests.cs
[TestFixture]
public sealed class DispatchServiceTests
{
    private IDispatchService _sut;
    private Mock<IStaffRepository> _staffRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _staffRepositoryMock = new Mock<IStaffRepository>();
        _sut = new DispatchService(_staffRepositoryMock.Object);
    }

    [Test]
    public async Task FindOptimalStaff_WhenSkillsMatch_ReturnsHigherSkillScoreFirst()
    {
        // Arrange
        // ...

        // Act
        var result = await _sut.FindOptimalStaffAsync(complaint, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result[0].SkillScore, Is.GreaterThan(result[1].SkillScore));
    }

    [Test]
    public async Task FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList()
    {
        // Arrange + Act + Assert
    }

    [Test]
    public async Task FindOptimalStaff_WhenUrgencyIsSosEmergency_DoublesMatchScore()
    {
        // Arrange + Act + Assert
    }
}
```

Rules:
- Use `[TestFixture]` on the class, `[Test]` on each test method.
- Use `[SetUp]` for common setup. Use `[TearDown]` for cleanup if needed.
- Use `NSubstitute` or `Moq` for mocking — not both in the same project.
- Use `FluentAssertions` for assertions where the fluent syntax improves readability.
- Arrange/Act/Assert sections separated by a blank line and an `// Arrange`, `// Act`, `// Assert` comment.
- Each test tests exactly one behaviour. Never assert multiple unrelated things in one test.
- Test projects use `[assembly: Parallelizable(ParallelScope.All)]` for speed.

---

## 3. TypeScript / React Standards (`ResidentApp.Web`)

### 3.1 Language Version and Config

- TypeScript strict mode: **enabled** (`"strict": true` in `tsconfig.json`)
- No `any`. Use `unknown` when the type is genuinely unknown, then narrow it.
- No type assertions (`as SomeType`) without a comment explaining why it is safe.
- ESLint + Prettier enforced via `packages/typescript-config`.

### 3.2 Naming

| Concept | Convention | Example |
|---|---|---|
| Components | PascalCase | `ComplaintCard`, `StaffAvailabilityBadge` |
| Hooks | `use` prefix, camelCase | `useComplaints`, `useAssignComplaint` |
| Utility functions | camelCase verb | `formatTat`, `mapTicketStatusToLabel` |
| Types and interfaces | PascalCase | `ComplaintDto`, `AssignComplaintRequest` |
| Enums | PascalCase type, SCREAMING_SNAKE values (match backend) | `TicketStatus.IN_PROGRESS` |
| Constants | SCREAMING_SNAKE | `MAX_MEDIA_ATTACHMENTS` |
| Files (components) | PascalCase `.tsx` | `ComplaintCard.tsx` |
| Files (hooks, utils) | camelCase `.ts` | `useComplaints.ts`, `formatTat.ts` |
| API client functions | camelCase verb phrase | `assignComplaint`, `getDispatchRecommendations` |

### 3.3 Component Structure

```tsx
// ComplaintCard.tsx
import type { ComplaintDto } from '@acls/api-contracts';

interface ComplaintCardProps {
  complaint: ComplaintDto;
  onAssign: (complaintId: number) => void;
}

export function ComplaintCard({ complaint, onAssign }: ComplaintCardProps) {
  return (
    <div>
      {/* render only — no business logic */}
    </div>
  );
}
```

Rules:
- Named exports, not default exports (improves refactoring and import traceability).
- Props interface defined immediately before the component. Never inline the type in the function signature.
- No business logic inside components. Components call hooks; hooks call API functions.
- No direct `fetch` calls inside components. All API calls go through `src/lib/api/`.

### 3.4 API Client Pattern

```typescript
// src/lib/api/complaints.ts
import { apiClient } from './client';
import type { ComplaintDto, AssignComplaintRequest } from '@acls/api-contracts';

export async function assignComplaint(
  complaintId: number,
  request: AssignComplaintRequest
): Promise<ComplaintDto> {
  const response = await apiClient.post<ComplaintDto>(
    `/complaints/${complaintId}/assign`,
    request
  );
  return response.data;
}
```

The `apiClient` is an Axios instance configured in `src/lib/api/client.ts` with the base URL from `process.env.NEXT_PUBLIC_API_URL`. It attaches the JWT bearer token from the auth context on every request.

### 3.5 No Business Logic on the Frontend

This bears repeating as a code-level standard:

```typescript
// WRONG — sorting on the frontend
const sortedComplaints = complaints.sort((a, b) =>
  a.urgency === 'SOS_EMERGENCY' ? -1 : 1
);

// CORRECT — display exactly what the API returned
const complaints = await getComplaints(); // already sorted by the backend
```

```typescript
// WRONG — computing TAT on the frontend
const tat = new Date(complaint.resolvedAt).getTime() -
            new Date(complaint.createdAt).getTime();

// CORRECT — display the tat field the API returns
const tat = complaint.tat; // computed by ACLS.Worker, returned in the DTO
```

---

## 4. Kotlin Android Standards (`ResidentApp.Android`, `StaffApp.Android`)

### 4.1 Language Version and Config

- Kotlin version: as specified in `build.gradle.kts` at project root.
- `minSdk`: 26 (Android 8.0)
- All coroutines use `Dispatchers.IO` for network/disk, `Dispatchers.Main` for UI.
- Hilt for dependency injection — no manual DI wiring.

### 4.2 Naming

| Concept | Convention | Example |
|---|---|---|
| Classes | PascalCase | `ComplaintRepository`, `StaffMember` |
| Data classes (DTOs) | PascalCase + `Dto` suffix | `ComplaintDto`, `StaffScoreDto` |
| Interfaces | PascalCase (no `I` prefix — Kotlin convention) | `ComplaintRepository` (interface) |
| Composable functions | PascalCase | `ComplaintCard`, `StaffAvailabilityBadge` |
| ViewModels | PascalCase + `ViewModel` suffix | `ComplaintsViewModel` |
| Retrofit functions | camelCase verb | `assignComplaint`, `getComplaints` |
| Variables and properties | camelCase | `complaintId`, `matchScore` |
| Constants | SCREAMING_SNAKE in `companion object` | `MAX_MEDIA_ATTACHMENTS` |
| Packages | lowercase, no underscores | `com.acls.resident.ui.screens` |

### 4.3 ViewModel Pattern

```kotlin
// ComplaintsViewModel.kt
@HiltViewModel
class ComplaintsViewModel @Inject constructor(
    private val complaintRepository: ComplaintRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow<ComplaintsUiState>(ComplaintsUiState.Loading)
    val uiState: StateFlow<ComplaintsUiState> = _uiState.asStateFlow()

    fun loadComplaints() {
        viewModelScope.launch {
            _uiState.value = ComplaintsUiState.Loading
            complaintRepository.getComplaints()
                .onSuccess { complaints ->
                    _uiState.value = ComplaintsUiState.Success(complaints)
                }
                .onFailure { error ->
                    _uiState.value = ComplaintsUiState.Error(error.message ?: "Unknown error")
                }
        }
    }
}
```

Rules:
- ViewModels expose `StateFlow` — never `LiveData`.
- ViewModels do not format strings, sort lists, or apply business rules. They call repository methods and expose state.
- Composables collect from `StateFlow` using `collectAsStateWithLifecycle()`.
- No direct Retrofit calls in Composables or ViewModels — all network calls go through repository implementations.

### 4.4 Retrofit API Interface

```kotlin
// ApiService.kt
interface ApiService {
    @POST("complaints/{complaintId}/assign")
    suspend fun assignComplaint(
        @Path("complaintId") complaintId: Int,
        @Body request: AssignComplaintRequest
    ): ComplaintDto

    @GET("complaints")
    suspend fun getComplaints(): List<ComplaintDto>
}
```

The base URL is read from `BuildConfig.API_BASE_URL`, which is set from `local.properties` for development and from CI environment variables for production. It is never hardcoded.

### 4.5 Sealed UI State

Every screen's state is modelled as a sealed class:

```kotlin
sealed class ComplaintsUiState {
    object Loading : ComplaintsUiState()
    data class Success(val complaints: List<ComplaintDto>) : ComplaintsUiState()
    data class Error(val message: String) : ComplaintsUiState()
}
```

Composables switch on this state using `when`:

```kotlin
when (val state = uiState.collectAsStateWithLifecycle().value) {
    is ComplaintsUiState.Loading -> LoadingIndicator()
    is ComplaintsUiState.Success -> ComplaintsList(state.complaints)
    is ComplaintsUiState.Error -> ErrorMessage(state.message)
}
```

---

## 5. Swift iOS Standards (`ResidentApp.iOS`, `StaffApp.iOS`)

### 5.1 Language Version and Config

- Swift version: as specified in the Xcode project.
- Minimum iOS deployment target: iOS 16.
- Swift concurrency (`async`/`await`) for all asynchronous operations — no completion handlers.
- `@MainActor` on ViewModels that update `@Published` properties.

### 5.2 Naming

| Concept | Convention | Example |
|---|---|---|
| Types (structs, classes, enums) | PascalCase | `ComplaintDto`, `TicketStatus` |
| Protocols | PascalCase (no `I` prefix — Swift convention) | `ComplaintRepository` |
| SwiftUI Views | PascalCase + `View` suffix | `ComplaintCardView`, `ComplaintsListView` |
| ViewModels | PascalCase + `ViewModel` suffix | `ComplaintsViewModel` |
| Functions and methods | camelCase verb | `assignComplaint()`, `loadComplaints()` |
| Properties | camelCase | `complaintId`, `matchScore`, `createdAt` |
| Constants | camelCase in enum namespace or `let` constants | `ComplaintConstants.maxMediaAttachments` |
| Files | Match the primary type | `ComplaintCardView.swift`, `ComplaintsViewModel.swift` |

### 5.3 ViewModel Pattern

```swift
// ComplaintsViewModel.swift
@MainActor
final class ComplaintsViewModel: ObservableObject {
    @Published private(set) var complaints: [ComplaintDto] = []
    @Published private(set) var isLoading = false
    @Published private(set) var errorMessage: String?

    private let repository: ComplaintRepository

    init(repository: ComplaintRepository) {
        self.repository = repository
    }

    func loadComplaints() async {
        isLoading = true
        errorMessage = nil
        do {
            complaints = try await repository.getComplaints()
        } catch {
            errorMessage = error.localizedDescription
        }
        isLoading = false
    }
}
```

Rules:
- ViewModels are `@MainActor` and `final`.
- `@Published` properties are `private(set)` — Views cannot write to them directly.
- ViewModels do not format strings, compute derived values, or sort lists.
- Views call ViewModel methods; ViewModels call repository methods.

### 5.4 API Client

```swift
// APIClient.swift
struct APIClient {
    private let baseURL: URL
    private let session: URLSession

    init() {
        guard let url = URL(string: Configuration.apiBaseURL) else {
            fatalError("Invalid API base URL in configuration")
        }
        self.baseURL = url
        self.session = URLSession.shared
    }

    func get<T: Decodable>(_ path: String) async throws -> T {
        let request = try buildRequest(path: path, method: "GET")
        let (data, response) = try await session.data(for: request)
        try validateResponse(response)
        return try JSONDecoder.acls.decode(T.self, from: data)
    }
}
```

`Configuration.apiBaseURL` reads from `Info.plist`, which is populated at build time from environment-specific configuration — never hardcoded.

`JSONDecoder.acls` is a custom extension that configures `keyDecodingStrategy = .convertFromSnakeCase` and `dateDecodingStrategy = .iso8601` to match the backend's JSON format.

### 5.5 Codable DTOs

```swift
// ComplaintDto.swift
struct ComplaintDto: Codable, Identifiable {
    let id: Int
    let complaintId: Int
    let title: String
    let description: String
    let urgency: Urgency
    let status: TicketStatus
    let createdAt: Date
    let resolvedAt: Date?
    let eta: Date?
    let tat: Double?          // minutes, nil until resolved
    let mediaUrls: [String]
}
```

Rules:
- All DTOs are `struct`, not `class`.
- All DTOs are `Codable`.
- `Identifiable` on DTOs that are displayed in SwiftUI `List`.
- Enum types (`TicketStatus`, `Urgency`, `StaffState`) are `String`, `Codable`, matching the backend's string values exactly.

---

## 6. Configuration and Secrets Standards (All Platforms)

### Backend (`appsettings.json`)

```json
// appsettings.json — committed to source control
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "acls-api",
    "Audience": "acls-clients",
    "ExpiryMinutes": 60
  },
  "Storage": {
    "ConnectionString": ""
  },
  "Notification": {
    "ApiKey": ""
  }
}
```

```json
// appsettings.Development.json — gitignored, never committed
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ACLS_Dev;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "your-local-dev-secret-min-32-chars"
  }
}
```

### Android (`local.properties`)

```
# local.properties — gitignored
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

### iOS (`Configuration.xcconfig`)

```
// Debug.xcconfig — gitignored
API_BASE_URL = http://localhost:5000/api/v1/
```

### React (`env.local`)

```
# .env.local — gitignored
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
```

---

## 7. Forbidden Patterns

These patterns must never appear in generated code. If you produce code containing any of them, the output is incomplete and must be corrected before delivery.

| Forbidden pattern | Correct alternative |
|---|---|
| Hardcoded connection string | Read from environment variable / `appsettings.Development.json` |
| Hardcoded API key or secret | Read from environment variable |
| `DbContext` referenced in `ACLS.Domain` or `ACLS.Application` | Use repository interface; implement in `ACLS.Persistence` |
| Business logic in a controller action body | Move to command/query handler |
| Business logic in a React component | Move to an API call; the server handles it |
| Business logic in a ViewModel (Android or iOS) | Move to backend; ViewModel only manages UI state |
| `byte[]` or `varbinary` column for media | Store blob URL string in `Media.Url` |
| `PropertyId` read from request body | Read from `ICurrentPropertyContext` injected by `TenancyMiddleware` |
| `DateTime.Now` | Use `DateTime.UtcNow` |
| `// TODO` without a `throw new NotImplementedException(...)` | Either implement it or throw explicitly |
| Swallowing exceptions with empty `catch {}` | Log and rethrow, or return `Result.Failure` |
| `public set` on a domain entity property | Use `private set` and expose state changes through methods |
| Two classes in one file | One class per file |
| Sorting or filtering API response data client-side | Return sorted/filtered data from the backend |
| `any` type in TypeScript | Use specific type or `unknown` with narrowing |
| Raw SQL in repository implementations without approval | Use LINQ / EF Core |
| `Thread.Sleep` or blocking `.Result` / `.Wait()` on tasks | Use `await` |
| Service locator pattern (`serviceProvider.GetService<T>()` in a class) | Constructor injection |

---

*End of Coding Standards v1.0*
