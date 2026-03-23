# Clean Architecture Guide

**Document:** `docs/07_Implementation/clean_architecture_guide.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document governs the structure of the entire `backend/` directory. Every class generated for the backend must be placed in the correct project and namespace as defined here. The dependency rule described in Section 2 must never be violated — not for convenience, not to save time, not because "it's just one method." A single violation compounds into architectural decay across sessions.

---

## 1. Why Clean Architecture

The ACLS backend is structured using Clean Architecture (also called Onion Architecture or Ports and Adapters). The purpose is a single, non-negotiable principle:

> **Business logic must not depend on infrastructure.**

This means the rules that govern how a complaint is assigned, how the dispatch algorithm ranks staff, how the ticket status machine transitions — none of these may depend on SQL Server, Azure Blob Storage, a notification provider, or ASP.NET Core's HTTP pipeline. If tomorrow the database changes from MSSQL to PostgreSQL, or the storage changes from Azure Blob to AWS S3, the domain and application logic must not change at all.

This is enforced structurally through project references. If `ACLS.Domain` cannot reference Entity Framework Core, it is physically impossible for a domain entity to contain a LINQ query. The compiler enforces the rule, not developer discipline.

---

## 2. The Dependency Rule

Dependencies point inward only. Outer layers know about inner layers. Inner layers know nothing about outer layers.

```
┌─────────────────────────────────────────────────────────────┐
│                        ACLS.Api                             │
│              (HTTP, Controllers, Middleware)                 │
│                           │                                 │
│              depends on   ▼                                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │               ACLS.Application                       │   │
│  │    (Use Cases, Commands, Queries, Validators)        │   │
│  │                      │                               │   │
│  │         depends on   ▼                               │   │
│  │   ┌──────────────────────────────────────────────┐   │   │
│  │   │              ACLS.Domain                     │   │   │
│  │   │  (Entities, Interfaces, Domain Events,       │   │   │
│  │   │   Business Rules, Enums, Value Objects)      │   │   │
│  │   │                    │                         │   │   │
│  │   │       depends on   ▼                         │   │   │
│  │   │   ┌──────────────────────────────────────┐   │   │   │
│  │   │   │         ACLS.SharedKernel            │   │   │   │
│  │   │   │  (EntityBase, Result<T>, Error,      │   │   │   │
│  │   │   │   IDomainEvent, Guard, strongly-     │   │   │   │
│  │   │   │   typed ID base classes)             │   │   │   │
│  │   │   └──────────────────────────────────────┘   │   │   │
│  │   └──────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  ACLS.Infrastructure  ──────────────────────► ACLS.Domain  │
│  (Storage, Notifications, Dispatch impl.)    (via interfaces)│
│                                                             │
│  ACLS.Persistence  ─────────────────────────► ACLS.Domain  │
│  (EF Core DbContext, Repositories)          (via interfaces)│
│                                                             │
│  ACLS.Worker  ──────────────────────────► ACLS.Application │
│  (Background jobs, async event handlers)                    │
│                                                             │
│  ACLS.Contracts  ───── (no dependencies — shared DTOs)     │
└─────────────────────────────────────────────────────────────┘
```

### 2.1 Allowed Project References (Enforced)

| Project | May reference | Must NEVER reference |
|---|---|---|
| `ACLS.SharedKernel` | _(nothing)_ | Everything |
| `ACLS.Domain` | `ACLS.SharedKernel` | Application, Infrastructure, Persistence, Api, Contracts |
| `ACLS.Application` | `ACLS.Domain`, `ACLS.SharedKernel` | Infrastructure, Persistence, Api |
| `ACLS.Infrastructure` | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` | Api, Persistence |
| `ACLS.Persistence` | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` | Api, Infrastructure |
| `ACLS.Api` | `ACLS.Application`, `ACLS.Contracts` | Domain (directly), Infrastructure, Persistence |
| `ACLS.Contracts` | _(nothing)_ | Everything |
| `ACLS.Worker` | `ACLS.Application`, `ACLS.SharedKernel` | Api, Infrastructure, Persistence (directly) |

### 2.2 How to Check You Are in the Right Layer

Before placing a class, ask these questions in order:

**Question 1:** Does this class represent a business concept — an entity, a rule, an interface for an external service, a domain event, or a value object?
→ **Yes** → `ACLS.Domain`

**Question 2:** Does this class represent a use case — a command, query, handler, validator, or DTO that orchestrates domain objects to fulfil a request?
→ **Yes** → `ACLS.Application`

**Question 3:** Does this class implement an interface defined in Domain or Application using an external technology — EF Core, Azure Blob, a notification SDK?
→ **Yes, it uses EF Core or SQL** → `ACLS.Persistence`
→ **Yes, it uses any other external technology** → `ACLS.Infrastructure`

**Question 4:** Does this class handle HTTP — receive requests, return responses, apply auth attributes, configure routing?
→ **Yes** → `ACLS.Api`

**Question 5:** Does this class represent a shared request/response DTO consumed by the API and external clients?
→ **Yes** → `ACLS.Contracts`

**Question 6:** Does this class execute a background job — async event processing, scheduled task, Worker Service hosted job?
→ **Yes** → `ACLS.Worker`

**Question 7:** Does this class provide base types, primitives, and utilities with zero external dependencies — `EntityBase`, `Result<T>`, `Error`, `IDomainEvent`, guard clauses?
→ **Yes** → `ACLS.SharedKernel`

If none of the above match, state the uncertainty explicitly before proceeding.

---

## 3. Project Responsibilities (Detailed)

### 3.1 `ACLS.SharedKernel`

**Purpose:** Foundation types used by every other project. Zero NuGet dependencies beyond the .NET BCL.

**Contains:**
```
ACLS.SharedKernel/
├── EntityBase.cs               # Base class for all domain entities
│                               # Holds List<IDomainEvent>, exposes RaiseDomainEvent()
├── IDomainEvent.cs             # Marker interface for domain events
├── Result.cs                   # Result<T> and Result (void) for operation outcomes
├── Error.cs                    # Immutable error value: Code (string) + Message (string)
├── Guard.cs                    # Precondition helpers: Guard.Against.Null(), Guard.Against.NegativeOrZero()
└── ValueObject.cs              # Base class for value objects (equality by value, not reference)
```

**Must NOT contain:** Any NuGet package references. No EF Core, no MediatR, no ASP.NET Core, no FluentValidation.

**`Result<T>` contract:**
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }           // throws if IsFailure
    public Error Error { get; }       // throws if IsSuccess

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    public static Result Success() => new(true);
    public static Result Failure(Error error) => new(error);
}
```

---

### 3.2 `ACLS.Domain`

**Purpose:** The heart of the system. Contains everything the business owns. Has no knowledge of how data is stored, how HTTP works, or which notification provider is used.

**Contains:**
```
ACLS.Domain/
├── Identity/
│   ├── User.cs
│   ├── Role.cs                     # enum: Resident, Manager, MaintenanceStaff
│   ├── InvitationToken.cs
│   ├── IUserRepository.cs
│   └── Events/
│       └── UserRegisteredEvent.cs
│
├── Properties/
│   ├── Property.cs
│   ├── Building.cs
│   ├── Unit.cs
│   ├── PropertyId.cs               # Strongly-typed value object wrapping int
│   ├── IPropertyRepository.cs
│   └── Events/
│
├── Residents/
│   ├── Resident.cs
│   ├── IResidentRepository.cs
│   └── Events/
│
├── Staff/
│   ├── StaffMember.cs
│   ├── StaffState.cs               # enum: AVAILABLE, BUSY, ON_BREAK, OFF_DUTY
│   ├── IStaffRepository.cs
│   └── Events/
│       └── StaffAvailabilityChangedEvent.cs
│
├── Complaints/
│   ├── Complaint.cs                # Aggregate root
│   ├── TicketStatus.cs             # enum: OPEN, ASSIGNED, EN_ROUTE, IN_PROGRESS, RESOLVED, CLOSED
│   ├── Urgency.cs                  # enum: LOW, MEDIUM, HIGH, SOS_EMERGENCY
│   ├── Media.cs
│   ├── WorkNote.cs
│   ├── ComplaintErrors.cs          # Static error definitions
│   ├── ComplaintConstants.cs       # MaxMediaAttachments = 3, etc.
│   ├── IComplaintRepository.cs
│   └── Events/
│       ├── ComplaintSubmittedEvent.cs
│       ├── ComplaintAssignedEvent.cs
│       ├── ComplaintStatusChangedEvent.cs
│       └── ComplaintResolvedEvent.cs
│
├── Dispatch/
│   ├── IDispatchService.cs         # Interface only — implementation in Infrastructure
│   ├── StaffScore.cs               # Value object: StaffMember + MatchScore + components
│   └── DispatchCriteria.cs         # Value object: required skills, urgency
│
├── Notifications/
│   ├── INotificationService.cs     # Interface only — implementation in Infrastructure
│   ├── NotificationChannel.cs      # enum: Email, SMS, InApp, Push
│   └── NotificationTemplate.cs
│
├── Outages/
│   ├── Outage.cs
│   ├── OutageType.cs               # enum: Electricity, Water, Gas, Internet, Elevator, Other
│   ├── IOutageRepository.cs
│   └── Events/
│       └── OutageDeclaredEvent.cs
│
├── Storage/
│   └── IStorageService.cs          # Interface only — implementation in Infrastructure
│
├── AuditLog/
│   ├── AuditEntry.cs
│   ├── AuditAction.cs              # enum of all audit actions
│   └── IAuditRepository.cs
│
└── Reporting/
    ├── IReportingService.cs        # Interface only
    └── StaffPerformanceSummary.cs  # Read model
```

**Rules:**
- No `using Microsoft.EntityFrameworkCore` anywhere in this project.
- No `using MediatR` anywhere in this project.
- No `using Microsoft.AspNetCore` anywhere in this project.
- Entities use `private set` on all properties.
- Entities expose state changes through methods that enforce invariants and raise domain events.
- Interfaces define the contract; they do not hint at implementation.

---

### 3.3 `ACLS.Application`

**Purpose:** Orchestrates domain objects to fulfil use cases. Knows the domain. Does not know how to send an email, write to a database, or handle HTTP. Receives commands and queries; returns Results.

**NuGet dependencies:** `MediatR`, `FluentValidation`, `AutoMapper` (or manual mapping — decide per project and document in ADR).

**Contains:**
```
ACLS.Application/
├── Complaints/
│   ├── Commands/
│   │   ├── SubmitComplaint/
│   │   │   ├── SubmitComplaintCommand.cs
│   │   │   ├── SubmitComplaintCommandHandler.cs
│   │   │   └── SubmitComplaintValidator.cs
│   │   ├── AssignComplaint/
│   │   │   ├── AssignComplaintCommand.cs
│   │   │   ├── AssignComplaintCommandHandler.cs
│   │   │   └── AssignComplaintValidator.cs
│   │   ├── ResolveComplaint/
│   │   │   ├── ResolveComplaintCommand.cs
│   │   │   ├── ResolveComplaintCommandHandler.cs
│   │   │   └── ResolveComplaintValidator.cs
│   │   ├── UpdateComplaintStatus/
│   │   ├── AddWorkNote/
│   │   ├── UpdateEta/
│   │   ├── TriggerSos/
│   │   ├── SubmitFeedback/
│   │   └── ReassignComplaint/
│   ├── Queries/
│   │   ├── GetComplaintById/
│   │   ├── GetComplaintsByResident/
│   │   ├── GetAllComplaints/
│   │   └── GetComplaintHistory/
│   └── DTOs/
│       ├── ComplaintDto.cs
│       ├── ComplaintSummaryDto.cs
│       └── MediaDto.cs
│
├── Dispatch/
│   ├── Queries/
│   │   └── GetDispatchRecommendations/
│   │       ├── GetDispatchRecommendationsQuery.cs
│   │       └── GetDispatchRecommendationsQueryHandler.cs
│   └── DTOs/
│       └── StaffScoreDto.cs
│
├── Staff/
│   ├── Commands/
│   │   └── UpdateAvailability/
│   ├── Queries/
│   │   ├── GetStaffAvailability/
│   │   └── GetAllStaff/
│   └── DTOs/
│       └── StaffDto.cs
│
├── Identity/
│   ├── Commands/
│   │   ├── RegisterResident/
│   │   └── LoginUser/
│   ├── Queries/
│   │   └── GetCurrentUser/
│   └── DTOs/
│       ├── UserDto.cs
│       └── AuthTokenDto.cs
│
├── Outages/
│   ├── Commands/
│   │   └── DeclareOutage/
│   ├── Queries/
│   │   └── GetOutagesByProperty/
│   └── DTOs/
│       └── OutageDto.cs
│
├── Reporting/
│   ├── Queries/
│   │   ├── GetStaffPerformanceSummary/
│   │   ├── GetComplaintsByUnit/
│   │   ├── GetComplaintSummaryReport/
│   │   └── GetDashboardMetrics/
│   └── DTOs/
│       ├── DashboardMetricsDto.cs
│       ├── StaffPerformanceSummaryDto.cs
│       └── UnitComplaintHistoryDto.cs
│
├── UserManagement/
│   ├── Commands/
│   │   ├── InviteResident/
│   │   ├── DeactivateUser/
│   │   └── ReactivateUser/
│   └── DTOs/
│       └── InvitationDto.cs
│
└── Common/
    ├── Behaviours/
    │   ├── ValidationBehaviour.cs      # MediatR pipeline: runs FluentValidation before handler
    │   ├── LoggingBehaviour.cs         # MediatR pipeline: structured request/response logging
    │   └── TransactionBehaviour.cs     # MediatR pipeline: wraps commands in DB transaction
    └── Interfaces/
        └── ICurrentPropertyContext.cs  # Exposes PropertyId and UserId for the current request
```

**Rules:**
- Handlers receive injected interfaces (`IComplaintRepository`, `IStorageService`) — never concrete implementations.
- Handlers never instantiate `DbContext`.
- Handlers never call `new HttpClient()` or any HTTP client directly.
- `ICurrentPropertyContext` is injected into any handler that needs `PropertyId`.
- Commands that mutate multiple entities (AssignComplaint, ResolveComplaint) use `TransactionBehaviour` to ensure atomicity.

**Command handler skeleton:**
```csharp
// AssignComplaintCommandHandler.cs
internal sealed class AssignComplaintCommandHandler
    : IRequestHandler<AssignComplaintCommand, Result<ComplaintDto>>
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly ICurrentPropertyContext _propertyContext;
    private readonly IPublisher _publisher;

    public AssignComplaintCommandHandler(
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
        AssignComplaintCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load entities — always filtered by PropertyId
        var complaint = await _complaintRepository.GetByIdAsync(
            command.ComplaintId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (complaint is null)
            return Result.Failure<ComplaintDto>(
                ComplaintErrors.NotFound(command.ComplaintId));

        var staff = await _staffRepository.GetByIdAsync(
            command.StaffMemberId,
            _propertyContext.PropertyId,
            cancellationToken);

        if (staff is null)
            return Result.Failure<ComplaintDto>(
                StaffErrors.NotFound(command.StaffMemberId));

        // 2. Invoke domain methods (which enforce invariants and raise events)
        var assignResult = complaint.Assign(staff.StaffMemberId);
        if (assignResult.IsFailure)
            return Result.Failure<ComplaintDto>(assignResult.Error);

        staff.MarkBusy();  // sets Availability = BUSY, LastAssignedAt = UtcNow

        // 3. Persist — TransactionBehaviour wraps this in a transaction
        await _complaintRepository.UpdateAsync(complaint, cancellationToken);
        await _staffRepository.UpdateAsync(staff, cancellationToken);

        // 4. Publish domain events (notification triggered here)
        foreach (var domainEvent in complaint.DomainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);

        complaint.ClearDomainEvents();

        // 5. Return DTO
        return Result.Success(ComplaintDto.FromDomain(complaint));
    }
}
```

---

### 3.4 `ACLS.Infrastructure`

**Purpose:** Implements interfaces defined in Domain using external technologies that are NOT the database. Storage (Azure Blob / S3), notifications (email/SMS), dispatch algorithm.

**NuGet dependencies:** Azure Blob Storage SDK or AWS S3 SDK (whichever is configured), notification provider SDK (abstract — provider pluggable via configuration).

**Contains:**
```
ACLS.Infrastructure/
├── Storage/
│   └── AzureBlobStorageService.cs      # Implements IStorageService
├── Notifications/
│   ├── NotificationService.cs          # Implements INotificationService
│   ├── EmailNotificationProvider.cs    # Sends email via configured provider
│   └── SmsNotificationProvider.cs      # Sends SMS via configured provider
├── Dispatch/
│   └── DispatchService.cs              # Implements IDispatchService
│                                       # Contains the Smart Dispatch algorithm
└── DependencyInjection.cs              # Extension method: AddInfrastructure(services, config)
```

**Rules:**
- Every class in this project implements an interface from `ACLS.Domain`.
- No business logic that belongs in Domain lives here. `DispatchService` executes the algorithm defined in `docs/07_Implementation/patterns/dispatch_algorithm.md` — it does not redefine it.
- Configuration (connection strings, API keys) is injected via `IConfiguration` or `IOptions<T>` — never hardcoded.

---

### 3.5 `ACLS.Persistence`

**Purpose:** Implements database access using EF Core and MSSQL. Contains `DbContext`, all entity configurations, all repository implementations, and all migrations.

**NuGet dependencies:** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`.

**Contains:**
```
ACLS.Persistence/
├── AclsDbContext.cs                    # Single DbContext for the application
├── Configurations/                     # IEntityTypeConfiguration<T> per entity
│   ├── PropertyConfiguration.cs
│   ├── BuildingConfiguration.cs
│   ├── UnitConfiguration.cs
│   ├── UserConfiguration.cs
│   ├── ResidentConfiguration.cs
│   ├── StaffMemberConfiguration.cs
│   ├── ComplaintConfiguration.cs
│   ├── MediaConfiguration.cs
│   ├── WorkNoteConfiguration.cs
│   ├── InvitationTokenConfiguration.cs
│   ├── OutageConfiguration.cs
│   └── AuditEntryConfiguration.cs
├── Repositories/                       # IRepository implementations
│   ├── ComplaintRepository.cs
│   ├── StaffRepository.cs
│   ├── UserRepository.cs
│   ├── ResidentRepository.cs
│   ├── PropertyRepository.cs
│   ├── OutageRepository.cs
│   └── AuditRepository.cs
├── Migrations/                         # EF Core migration files (auto-generated)
│   └── 20260201_InitialSchema.cs
└── DependencyInjection.cs              # Extension method: AddPersistence(services, config)
```

**`AclsDbContext` skeleton:**
```csharp
public sealed class AclsDbContext : DbContext
{
    public AclsDbContext(DbContextOptions<AclsDbContext> options) : base(options) { }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<WorkNote> WorkNotes => Set<WorkNote>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();
    public DbSet<Outage> Outages => Set<Outage>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AclsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

**Rules:**
- `DbContext` is registered as `Scoped`.
- No raw SQL without explicit approval and documentation.
- All queries include `PropertyId` filter — see repository pattern in coding standards.
- Migrations are never edited manually after they are generated.
- `AuditRepository` exposes `AddAsync` only — no update or delete.

---

### 3.6 `ACLS.Api`

**Purpose:** The HTTP entry point. Receives requests, authenticates and authorises users, enforces tenancy, dispatches commands and queries via MediatR, and returns HTTP responses.

**NuGet dependencies:** `MediatR`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Swashbuckle.AspNetCore`.

**Contains:**
```
ACLS.Api/
├── Controllers/
│   ├── ApiControllerBase.cs            # Abstract base: [ApiController], [Authorize], route prefix
│   ├── ComplaintsController.cs
│   ├── StaffController.cs
│   ├── AuthController.cs
│   ├── OutagesController.cs
│   ├── ReportsController.cs
│   └── UsersController.cs
├── Middleware/
│   ├── TenancyMiddleware.cs            # Reads PropertyId from JWT → populates ICurrentPropertyContext
│   └── ExceptionHandlingMiddleware.cs  # Global exception handler → Problem Details (RFC 7807)
├── Services/
│   └── CurrentPropertyContext.cs       # Scoped implementation of ICurrentPropertyContext
│                                       # Registered here; interface defined in ACLS.Application
└── Program.cs                          # Composition root — all DI registration, middleware pipeline
```

**`Program.cs` registration order:**
```csharp
builder.Services.AddPersistence(builder.Configuration);      // ACLS.Persistence DI
builder.Services.AddInfrastructure(builder.Configuration);   // ACLS.Infrastructure DI
builder.Services.AddApplication();                           // ACLS.Application DI (MediatR, validators)
builder.Services.AddScoped<ICurrentPropertyContext, CurrentPropertyContext>();

// Middleware pipeline order (matters):
app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenancyMiddleware();     // must be after UseAuthentication — reads from authenticated JWT
app.MapControllers();
```

**Rules:**
- Controllers never reference `DbContext` or any persistence type.
- Controllers never reference `IStorageService`, `INotificationService`, or any infrastructure type.
- Controllers reference only `IMediator` and `ACLS.Contracts` types.
- `TenancyMiddleware` must run after `UseAuthentication()` — it reads a claim from the authenticated identity.

---

### 3.7 `ACLS.Contracts`

**Purpose:** Shared request and response DTOs that are the public API surface. Consumed by `ACLS.Api` (to bind HTTP requests and format responses) and by external clients (TypeScript SDK, Android, iOS).

**NuGet dependencies:** None. Plain C# classes only. Serialization attributes (`[JsonPropertyName]`) are permitted.

**Contains:**
```
ACLS.Contracts/
├── Complaints/
│   ├── SubmitComplaintRequest.cs
│   ├── AssignComplaintRequest.cs
│   ├── UpdateComplaintStatusRequest.cs
│   ├── ResolveComplaintRequest.cs
│   ├── AddWorkNoteRequest.cs
│   ├── UpdateEtaRequest.cs
│   ├── SubmitFeedbackRequest.cs
│   └── ReassignComplaintRequest.cs
├── Auth/
│   ├── RegisterResidentRequest.cs
│   └── LoginRequest.cs
├── Staff/
│   └── UpdateAvailabilityRequest.cs
├── Outages/
│   └── DeclareOutageRequest.cs
└── Users/
    └── InviteResidentRequest.cs
```

**Rules:**
- Contains only request models (inbound from client).
- Response models (outbound to client) live in `ACLS.Application/*/DTOs/` and are mapped in controllers.
- No business logic. No validation annotations (`[Required]` etc.) — validation is handled by FluentValidation in `ACLS.Application`.
- All property names use PascalCase in C#; JSON serialisation uses camelCase via global `JsonSerializerOptions`.

---

### 3.8 `ACLS.Worker`

**Purpose:** Background processing. Handles async work that must not block the HTTP request/response cycle: TAT calculation, average rating recalculation, notification fan-out for outages.

**NuGet dependencies:** `MediatR`, `Microsoft.Extensions.Hosting`.

**Contains:**
```
ACLS.Worker/
├── Jobs/
│   ├── CalculateTatJob.cs              # Triggered by ComplaintResolvedEvent
│   ├── UpdateAverageRatingJob.cs       # Triggered by FeedbackSubmittedEvent
│   └── BroadcastOutageNotificationJob.cs # Triggered by OutageDeclaredEvent — fan-out to all residents
├── EventHandlers/
│   ├── ComplaintResolvedEventHandler.cs  # INotificationHandler<ComplaintResolvedEvent>
│   ├── FeedbackSubmittedEventHandler.cs  # INotificationHandler<FeedbackSubmittedEvent>
│   └── OutageDeclaredEventHandler.cs     # INotificationHandler<OutageDeclaredEvent>
└── Program.cs                            # Worker host entry point
```

**Rules:**
- Worker jobs are triggered by domain events via MediatR's `INotificationHandler<T>`.
- Workers call Application layer commands/queries via `IMediator` — they do not access repositories or `DbContext` directly.
- `BroadcastOutageNotificationJob` must fan out notifications without blocking: fetches all resident contact details, dispatches notification calls concurrently (respecting NFR-12: 500 messages in 60 seconds).

---

## 4. The Flow of a Request End to End

This is what happens when a Manager calls `POST /api/v1/complaints/{id}/assign`. Trace this flow mentally when placing any new class.

```
1. HTTP POST arrives at ACLS.Api

2. TenancyMiddleware reads PropertyId from JWT claim
   → populates ICurrentPropertyContext (scoped service)

3. ComplaintsController.AssignComplaint() receives the request
   → maps to AssignComplaintCommand(complaintId, staffMemberId)
   → sends via IMediator.Send(command)

4. ValidationBehaviour (MediatR pipeline) runs AssignComplaintValidator
   → returns 400 if invalid

5. TransactionBehaviour (MediatR pipeline) opens a DB transaction

6. AssignComplaintCommandHandler.Handle() executes:
   a. Calls IComplaintRepository.GetByIdAsync(complaintId, propertyId)
      → ACLS.Persistence: ComplaintRepository queries AclsDbContext.Complaints
        WHERE ComplaintId = @id AND PropertyId = @propertyId
   b. Calls IStaffRepository.GetByIdAsync(staffMemberId, propertyId)
   c. Calls complaint.Assign(staffMemberId)    ← domain method, raises ComplaintAssignedEvent
   d. Calls staff.MarkBusy()                  ← domain method, sets Availability = BUSY
   e. Calls IComplaintRepository.UpdateAsync()
   f. Calls IStaffRepository.UpdateAsync()
   g. Publishes ComplaintAssignedEvent via IPublisher

7. TransactionBehaviour commits the transaction
   (both complaint and staff writes committed atomically)

8. ComplaintAssignedEventHandler (in ACLS.Worker or inline handler) receives event
   → calls INotificationService.NotifyStaff(staffMemberId, complaint)
      → ACLS.Infrastructure: NotificationService sends SMS/email to staff

9. ComplaintsController receives Result<ComplaintDto>
   → maps to HTTP 200 OK with ComplaintDto body
```

This flow demonstrates the inward dependency direction at every step. The controller never calls the repository. The handler never calls the notification service directly — it publishes an event. The notification service never knows about the database.

---

## 5. Common Violations and How to Detect Them

| Violation | How it manifests | Correct fix |
|---|---|---|
| EF Core in Domain | `using Microsoft.EntityFrameworkCore` in a `ACLS.Domain` file | Move the query to `ACLS.Persistence`. Define an interface in Domain |
| Business logic in controller | `if (urgency == SOS_EMERGENCY)` inside a controller action body | Move logic to a command handler in `ACLS.Application` |
| Repository called from controller | `_complaintRepository.GetByIdAsync(...)` in a controller | Controller sends MediatR command/query; handler calls repository |
| Infrastructure service in Application | `using Azure.Storage.Blobs` in `ACLS.Application` | Define `IStorageService` in Domain; inject it into the handler; implement in Infrastructure |
| `PropertyId` from request body | `[FromBody] int propertyId` on a controller parameter | Remove. Read from `ICurrentPropertyContext.PropertyId` injected by TenancyMiddleware |
| DbContext registered as Singleton | `services.AddSingleton<AclsDbContext>()` | Must be `AddDbContext<AclsDbContext>()` — which is Scoped by default |
| Domain event published before commit | `_publisher.Publish(event)` before `SaveChangesAsync()` | Publish after the transaction commits — see handler skeleton in Section 3.3 |
| Two entities updated in separate transactions | Two separate `UpdateAsync` calls with no `TransactionBehaviour` | Use `TransactionBehaviour` MediatR pipeline; both updates in one transaction |

---

## 6. Solution File and Project Reference Configuration

The solution file `backend/ACLS.sln` references all projects. Project references in `.csproj` files enforce the dependency rule at compile time.

```xml
<!-- ACLS.Domain.csproj — only SharedKernel -->
<ItemGroup>
  <ProjectReference Include="..\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />
</ItemGroup>

<!-- ACLS.Application.csproj — Domain and SharedKernel -->
<ItemGroup>
  <ProjectReference Include="..\ACLS.Domain\ACLS.Domain.csproj" />
  <ProjectReference Include="..\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />
</ItemGroup>

<!-- ACLS.Persistence.csproj — Application, Domain, SharedKernel, EF Core -->
<ItemGroup>
  <ProjectReference Include="..\ACLS.Application\ACLS.Application.csproj" />
  <ProjectReference Include="..\ACLS.Domain\ACLS.Domain.csproj" />
  <ProjectReference Include="..\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
</ItemGroup>

<!-- ACLS.Infrastructure.csproj — Application, Domain, SharedKernel, external SDKs -->
<ItemGroup>
  <ProjectReference Include="..\ACLS.Application\ACLS.Application.csproj" />
  <ProjectReference Include="..\ACLS.Domain\ACLS.Domain.csproj" />
  <ProjectReference Include="..\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />
  <!-- Storage SDK added here when provider is chosen -->
</ItemGroup>

<!-- ACLS.Api.csproj — Application, Contracts, Persistence (for DI registration only), Infrastructure (for DI registration only) -->
<ItemGroup>
  <ProjectReference Include="..\ACLS.Application\ACLS.Application.csproj" />
  <ProjectReference Include="..\ACLS.Contracts\ACLS.Contracts.csproj" />
  <ProjectReference Include="..\ACLS.Persistence\ACLS.Persistence.csproj" />
  <ProjectReference Include="..\ACLS.Infrastructure\ACLS.Infrastructure.csproj" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
</ItemGroup>
```

> **Note on Api referencing Persistence and Infrastructure:** `ACLS.Api` references `ACLS.Persistence` and `ACLS.Infrastructure` only for dependency injection registration in `Program.cs`. Controllers must never use types from these projects directly. This is the standard Clean Architecture composition root pattern.

---

*End of Clean Architecture Guide v1.0*
