# Testing Strategy

**Document:** `docs/07_Implementation/testing_strategy.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> NUnit is the mandatory test framework for this project. No xUnit, no MSTest. Every test class uses `[TestFixture]`. Every test method uses `[Test]`. Tests are never placed inside application project folders — they live exclusively under `tests/`. Read this document before generating any test class.

---

## 1. Testing Philosophy

Tests in ACLS serve three purposes in priority order:

1. **Specification** — Tests document what the system is supposed to do. A failing test means the implementation diverged from the spec, not that the spec is wrong.
2. **Regression protection** — Once a workflow is implemented and tested, it must not break silently when other parts of the system change.
3. **Design pressure** — Code that is hard to test is usually poorly designed. If writing a unit test requires excessive mocking, the class under test is doing too much.

**Tests are written alongside the code they verify — not in a separate "testing phase."** When a command handler is generated, its test class is generated in the same output. When a repository is generated, its integration test is generated in the same output.

---

## 2. Test Project Structure

```
tests/
├── unit/
│   ├── Domain.Tests/               # Tests for ACLS.Domain — pure logic, no I/O
│   │   ├── Complaints/
│   │   │   ├── ComplaintTests.cs
│   │   │   └── ComplaintStatusMachineTests.cs
│   │   └── Dispatch/
│   │       └── DispatchServiceTests.cs
│   └── Application.Tests/          # Tests for ACLS.Application handlers
│       ├── Complaints/
│       │   ├── SubmitComplaintCommandHandlerTests.cs
│       │   ├── AssignComplaintCommandHandlerTests.cs
│       │   └── ResolveComplaintCommandHandlerTests.cs
│       ├── Dispatch/
│       │   └── GetDispatchRecommendationsQueryHandlerTests.cs
│       └── Outages/
│           └── DeclareOutageCommandHandlerTests.cs
│
├── integration/
│   ├── Persistence.Tests/          # Tests with a real SQL Server via TestContainers
│   │   ├── Repositories/
│   │   │   ├── ComplaintRepositoryTests.cs
│   │   │   ├── StaffRepositoryTests.cs
│   │   │   └── AuditRepositoryTests.cs
│   │   └── Transactions/
│   │       ├── AssignComplaintTransactionTests.cs
│   │       └── ResolveComplaintTransactionTests.cs
│   └── Infrastructure.Tests/       # Tests for storage and notification adapters
│       └── Storage/
│           └── AzureBlobStorageServiceTests.cs
│
├── contract/
│   └── Api.ContractTests/          # Verify API response shapes match v1.yaml
│       └── ComplaintsContractTests.cs
│
├── e2e/
│   ├── Api.E2ETests/               # Full HTTP flow tests against running API
│   │   ├── ComplaintLifecycleTests.cs
│   │   └── MultiTenancyIsolationTests.cs
│   └── Web.E2ETests/               # Playwright tests for Manager Dashboard
│       └── DashboardTests.cs
│
└── performance/
    └── k6/
        └── outage_broadcast.js     # NFR-12: 500 notifications in 60 seconds
```

---

## 3. Test Types and Rules

### 3.1 Unit Tests (`tests/unit/`)

**What they test:** Pure business logic. No database, no HTTP, no file system, no external services.

**Speed target:** The entire unit test suite must complete in under 30 seconds.

**Rules:**
- All dependencies are mocked using NSubstitute.
- No `[SetUp]` method that touches a database or starts a process.
- No `async` test setup that involves network I/O.
- Tests are `[Parallelizable(ParallelScope.All)]`.
- Each test tests exactly one behaviour. One `[Test]` method = one assertion path.

**NuGet packages:**
```xml
<PackageReference Include="NUnit" Version="4.*" />
<PackageReference Include="NUnit3TestAdapter" Version="4.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
```

**Project references:**
```xml
<!-- Domain.Tests.csproj -->
<ProjectReference Include="..\..\backend\ACLS.Domain\ACLS.Domain.csproj" />
<ProjectReference Include="..\..\backend\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />

<!-- Application.Tests.csproj -->
<ProjectReference Include="..\..\backend\ACLS.Application\ACLS.Application.csproj" />
<ProjectReference Include="..\..\backend\ACLS.Domain\ACLS.Domain.csproj" />
<ProjectReference Include="..\..\backend\ACLS.SharedKernel\ACLS.SharedKernel.csproj" />
```

---

### 3.2 Integration Tests (`tests/integration/`)

**What they test:** Persistence layer (real SQL), infrastructure adapters (real Azurite), transaction atomicity.

**Speed target:** Full integration suite in under 3 minutes.

**Rules:**
- Use TestContainers to spin up a real SQL Server container per test run.
- Use `Respawn` to reset database state between tests (faster than recreating the container).
- Do not mock `AclsDbContext` — use the real context against a test database.
- Each test class inherits from `IntegrationTestBase` which handles container lifecycle.
- Tests are not parallelised at the class level (database state is shared within a test run).

**NuGet packages:**
```xml
<PackageReference Include="NUnit" Version="4.*" />
<PackageReference Include="NUnit3TestAdapter" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="Testcontainers.MsSql" Version="3.*" />
<PackageReference Include="Respawn" Version="6.*" />
```

**`IntegrationTestBase` skeleton:**
```csharp
// tests/integration/Persistence.Tests/IntegrationTestBase.cs
[TestFixture]
public abstract class IntegrationTestBase
{
    private static MsSqlContainer _sqlContainer = null!;
    protected AclsDbContext Context { get; private set; } = null!;
    private Respawner _respawner = null!;

    [OneTimeSetUp]
    public static async Task StartContainer()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sqlContainer.StartAsync();
    }

    [OneTimeTearDown]
    public static async Task StopContainer()
        => await _sqlContainer.StopAsync();

    [SetUp]
    public async Task SetUpContext()
    {
        var options = new DbContextOptionsBuilder<AclsDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        Context = new AclsDbContext(options);
        await Context.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(
            _sqlContainer.GetConnectionString(),
            new RespawnerOptions
            {
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
    }

    [TearDown]
    public async Task ResetDatabase()
    {
        await _respawner.ResetAsync(_sqlContainer.GetConnectionString());
        await Context.DisposeAsync();
    }
}
```

---

### 3.3 Contract Tests (`tests/contract/`)

**What they test:** That the API's actual JSON responses match the shapes defined in `docs/05_API/openapi/v1.yaml`.

**Purpose:** Prevent the backend from silently changing a field name or type that the three frontend clients depend on.

**Rules:**
- Spin up the API in-process using `WebApplicationFactory<Program>`.
- Deserialise actual HTTP responses and assert field presence and types.
- Run against all endpoints in `v1.yaml` for the happy path.

---

### 3.4 E2E Tests (`tests/e2e/`)

**What they test:** Complete user workflows from HTTP request to database state.

**Rules:**
- `Api.E2ETests` spin up the full API stack using `WebApplicationFactory<Program>` with a TestContainers SQL Server.
- Tests exercise complete workflows: register → submit complaint → assign → update status → resolve → feedback.
- `Web.E2ETests` use Playwright against a running staging environment (not run on every commit — run on merge to `main`).

---

## 4. Mandatory Test Coverage by Feature

The following test cases are mandatory. Every case must exist and pass before the corresponding feature is considered complete.

---

### 4.1 Complaint Domain Entity (`Domain.Tests/Complaints/ComplaintTests.cs`)

| Test method | What it verifies |
|---|---|
| `Create_WithValidInputs_SetsStatusToOpen` | New complaint always starts as OPEN |
| `Create_WithValidInputs_SetsCreatedAtToUtcNow` | CreatedAt is set at creation time |
| `Assign_WhenStatusIsOpen_SetsStatusToAssigned` | Valid OPEN → ASSIGNED transition |
| `Assign_WhenStatusIsOpen_SetsAssignedStaffMemberId` | FK is set on assignment |
| `Assign_WhenStatusIsOpen_RaisesComplaintAssignedEvent` | Domain event is raised |
| `Assign_WhenStatusIsClosed_ReturnsFailureResult` | CLOSED complaint cannot be reassigned |
| `Assign_WhenStatusIsResolved_ReturnsFailureResult` | RESOLVED complaint cannot be reassigned |
| `Resolve_WhenStatusIsInProgress_SetsStatusToResolved` | Valid IN_PROGRESS → RESOLVED transition |
| `Resolve_WhenStatusIsInProgress_SetsResolvedAt` | ResolvedAt timestamp set on resolution |
| `Resolve_WhenStatusIsInProgress_RaisesComplaintResolvedEvent` | Domain event is raised |
| `Resolve_WhenStatusIsOpen_ReturnsFailureResult` | Cannot resolve unassigned complaint |
| `Close_WhenStatusIsResolved_SetsStatusToClosed` | Valid RESOLVED → CLOSED transition |
| `Close_WhenStatusIsOpen_ReturnsFailureResult` | Cannot close an OPEN complaint |
| `AddMedia_WhenUnderLimit_AddsMedia` | Media added when under MaxMediaAttachments |
| `AddMedia_WhenAtLimit_ReturnsFailureResult` | Cannot exceed MaxMediaAttachments |

---

### 4.2 Dispatch Service (`Domain.Tests/Dispatch/DispatchServiceTests.cs`)

All 12 tests specified in `docs/07_Implementation/patterns/dispatch_algorithm.md` Section 9. No additional tests needed beyond those 12.

---

### 4.3 Assign Complaint Handler (`Application.Tests/Complaints/AssignComplaintCommandHandlerTests.cs`)

| Test method | What it verifies |
|---|---|
| `Handle_WhenComplaintAndStaffExist_ReturnsSuccess` | Happy path returns ComplaintDto |
| `Handle_WhenComplaintNotFound_ReturnsNotFoundFailure` | 404 path via Result.Failure |
| `Handle_WhenStaffNotFound_ReturnsNotFoundFailure` | Staff not found returns failure |
| `Handle_WhenComplaintExists_SetsComplaintStatusToAssigned` | Status set correctly |
| `Handle_WhenComplaintExists_SetsStaffAvailabilityToBusy` | Staff marked BUSY |
| `Handle_WhenComplaintExists_PublishesDomainEvent` | ComplaintAssignedEvent published |
| `Handle_WhenComplaintBelongsToDifferentProperty_ReturnsNotFound` | Cross-property returns NotFound |

---

### 4.4 Resolve Complaint Handler (`Application.Tests/Complaints/ResolveComplaintCommandHandlerTests.cs`)

| Test method | What it verifies |
|---|---|
| `Handle_WhenComplaintIsInProgress_ReturnsSuccess` | Happy path |
| `Handle_WhenComplaintIsInProgress_SetsStatusToResolved` | Status transitions correctly |
| `Handle_WhenComplaintIsInProgress_SetsStaffAvailabilityToAvailable` | Staff freed |
| `Handle_WhenComplaintIsInProgress_SetsResolvedAt` | Timestamp set |
| `Handle_WhenComplaintIsInProgress_PublishesComplaintResolvedEvent` | Event published for notification |
| `Handle_WhenComplaintIsOpen_ReturnsInvalidTransitionFailure` | Cannot resolve OPEN complaint |
| `Handle_WhenComplaintIsAlreadyResolved_ReturnsInvalidTransitionFailure` | Idempotency guard |

---

### 4.5 Submit Complaint Handler (`Application.Tests/Complaints/SubmitComplaintCommandHandlerTests.cs`)

| Test method | What it verifies |
|---|---|
| `Handle_WithValidInputs_CreatesComplaintWithOpenStatus` | Initial status correct |
| `Handle_WithMediaUrls_CreatesMediaEntitiesWithUrlsOnly` | No binary in entities |
| `Handle_WithMediaUrls_DoesNotCallStorageService` | Storage called in controller, not handler |
| `Handle_WithValidInputs_SetsPropertyIdFromContext` | PropertyId from context, not request |
| `Handle_WithValidInputs_RaisesComplaintSubmittedEvent` | Event published |

---

### 4.6 Declare Outage Handler (`Application.Tests/Outages/DeclareOutageCommandHandlerTests.cs`)

| Test method | What it verifies |
|---|---|
| `Handle_WithValidInputs_CreatesOutageRecord` | Outage persisted |
| `Handle_WithValidInputs_PublishesOutageDeclaredEvent` | Event raised for Worker broadcast |
| `Handle_WithValidInputs_DoesNotCallNotificationServiceDirectly` | Notification is async via event |

---

### 4.7 Repository Integration Tests (`Persistence.Tests/Repositories/ComplaintRepositoryTests.cs`)

| Test method | What it verifies |
|---|---|
| `GetByIdAsync_WhenComplaintExists_ReturnsComplaint` | Basic retrieval |
| `GetByIdAsync_WhenComplaintNotFound_ReturnsNull` | Returns null not exception |
| `GetByIdAsync_WhenComplaintBelongsToDifferentProperty_ReturnsNull` | **Multi-tenancy isolation** |
| `GetAllAsync_WithPropertyFilter_ReturnsOnlyPropertyComplaints` | **Multi-tenancy isolation on list** |
| `GetAllAsync_WithStatusFilter_ReturnsFilteredComplaints` | Status filtering works |
| `GetAllAsync_WithDateRangeFilter_ReturnsFilteredComplaints` | Date filtering works |
| `GetAllAsync_WithPagination_ReturnsCorrectPage` | Pagination works |
| `AddAsync_WithValidComplaint_PersistsToDatabase` | Write path works |
| `UpdateAsync_WhenComplaintStatusChanged_PersistsNewStatus` | Update path works |

The multi-tenancy isolation tests are the most critical. Each repository must have at least one test that seeds data for Property 2 and verifies it is invisible when querying as Property 1.

---

### 4.8 Transaction Atomicity Tests (`Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs`)

| Test method | What it verifies |
|---|---|
| `AssignComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | Atomic write |
| `AssignComplaint_WhenStaffUpdateFails_ComplaintStatusNotChanged` | Rollback on failure |
| `ResolveComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | Atomic resolve |
| `ResolveComplaint_WhenComplaintUpdateFails_StaffAvailabilityNotChanged` | Rollback on failure |

These tests verify the atomicity guarantee from `ai_collaboration_rules.md` Rule 7 and Rule 8. They require a real database — they cannot be unit tested with mocks because they test the transaction behaviour of EF Core.

**Implementation approach:** Force a simulated failure by subclassing `AclsDbContext` to throw after the first `SaveChangesAsync` call, then verify that neither entity was updated.

---

### 4.9 E2E Complaint Lifecycle (`e2e/Api.E2ETests/ComplaintLifecycleTests.cs`)

| Test method | What it verifies |
|---|---|
| `FullLifecycle_SubmitAssignResolveAndFeedback_CompletesSuccessfully` | End-to-end happy path |
| `SosLifecycle_TriggerSos_ComplaintCreatedAndStaffNotified` | SOS end-to-end |
| `OutageLifecycle_DeclareOutage_OutageCreatedAndBroadcastQueued` | Outage end-to-end |

---

### 4.10 Multi-Tenancy E2E (`e2e/Api.E2ETests/MultiTenancyIsolationTests.cs`)

| Test method | What it verifies |
|---|---|
| `GetComplaint_WhenComplaintBelongsToDifferentProperty_Returns404` | Cross-property returns 404 |
| `GetAllComplaints_ReturnsOnlyAuthenticatedPropertyComplaints` | List is property-scoped |
| `AssignComplaint_WhenStaffBelongsToDifferentProperty_Returns404` | Cannot assign cross-property staff |

---

## 5. Test Naming Convention

All test methods follow this pattern:

```
<MethodUnderTest>_<Scenario>_<ExpectedOutcome>
```

Examples:
- `Handle_WhenComplaintNotFound_ReturnsNotFoundFailure`
- `GetByIdAsync_WhenComplaintBelongsToDifferentProperty_ReturnsNull`
- `FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList`
- `Assign_WhenStatusIsClosed_ReturnsFailureResult`

**Rules:**
- `<MethodUnderTest>` — the exact method name being tested
- `<Scenario>` — the condition being set up, starting with `When`
- `<ExpectedOutcome>` — what happens, using present tense

Do not use:
- `Test_` prefix
- `Should_` anywhere
- Generic names like `Test1`, `HappyPath`, `WorksCorrectly`

---

## 6. Test Structure — Arrange / Act / Assert

Every test follows the three-section pattern with comments as separators:

```csharp
[Test]
public async Task Handle_WhenComplaintExists_SetsStaffAvailabilityToBusy()
{
    // Arrange
    var complaint = CreateComplaint(status: TicketStatus.OPEN);
    var staff = CreateStaff(availability: StaffState.AVAILABLE);

    _complaintRepositoryMock
        .GetByIdAsync(complaint.ComplaintId, PropertyId, Arg.Any<CancellationToken>())
        .Returns(complaint);

    _staffRepositoryMock
        .GetByIdAsync(staff.StaffMemberId, PropertyId, Arg.Any<CancellationToken>())
        .Returns(staff);

    var command = new AssignComplaintCommand(complaint.ComplaintId, staff.StaffMemberId);

    // Act
    var result = await _sut.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    staff.Availability.Should().Be(StaffState.BUSY);

    await _staffRepositoryMock
        .Received(1)
        .UpdateAsync(staff, Arg.Any<CancellationToken>());
}
```

**Rules:**
- All three sections present in every test.
- Blank line between sections.
- No logic in the Assert section — one assertion concept per test.
- Use `FluentAssertions` (`.Should().Be()`) for readability.
- Use `NSubstitute` for mocking: `Arg.Any<T>()` for don't-care parameters, `.Received(1)` for interaction verification.

---

## 7. Test Data Helpers

Each test project defines a set of factory helper methods to create domain objects with sensible defaults. These are static methods in a `TestDataFactory` class in the test project.

```csharp
// tests/unit/Application.Tests/TestDataFactory.cs
internal static class TestDataFactory
{
    internal const int PropertyId = 1;
    internal const int ResidentId = 10;
    internal const int StaffMemberId = 20;
    internal const int UnitId = 5;

    internal static Complaint CreateComplaint(
        TicketStatus status = TicketStatus.OPEN,
        Urgency urgency = Urgency.MEDIUM,
        int? assignedStaffMemberId = null,
        List<string>? requiredSkills = null)
    {
        var complaint = Complaint.Create(
            title: "Test complaint",
            description: "Test description",
            category: "Plumbing",
            urgency: urgency,
            unitId: UnitId,
            residentId: ResidentId,
            propertyId: PropertyId,
            permissionToEnter: false);

        // Force status via reflection for testing non-OPEN initial states
        if (status != TicketStatus.OPEN)
            SetPrivateProperty(complaint, nameof(Complaint.Status), status);

        if (assignedStaffMemberId.HasValue)
            SetPrivateProperty(complaint, nameof(Complaint.AssignedStaffMemberId),
                assignedStaffMemberId.Value);

        if (requiredSkills is not null)
            SetPrivateProperty(complaint, nameof(Complaint.RequiredSkills), requiredSkills);

        return complaint;
    }

    internal static StaffMember CreateStaff(
        StaffState availability = StaffState.AVAILABLE,
        List<string>? skills = null,
        DateTime? lastAssignedAt = null)
    {
        var staff = new StaffMember
        {
            StaffMemberId = StaffMemberId,
            JobTitle = "Plumber",
            Skills = skills ?? ["Plumbing", "General"],
            Availability = availability,
            LastAssignedAt = lastAssignedAt
        };
        return staff;
    }

    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(obj, value);
    }
}
```

**Why `SetPrivateProperty`:** Domain entities have `private set` properties enforced by design. Tests that need to set up a specific pre-condition (e.g. a complaint already in `IN_PROGRESS` status) must use reflection to bypass the private setter. This is acceptable in tests — it is not acceptable in production code.

---

## 8. CI Pipeline Integration

```
Every commit to any branch:
  ├── dotnet test tests/unit/Domain.Tests
  └── dotnet test tests/unit/Application.Tests
  (must complete in < 30 seconds)

Every pull request:
  ├── Unit tests (above)
  ├── dotnet test tests/integration/Persistence.Tests    (TestContainers)
  ├── dotnet test tests/integration/Infrastructure.Tests
  └── dotnet test tests/contract/Api.ContractTests
  (must complete in < 3 minutes)

Merge to main:
  ├── All PR tests (above)
  └── dotnet test tests/e2e/Api.E2ETests
  (full lifecycle tests)

Release candidate:
  ├── All main tests (above)
  ├── Playwright Web.E2ETests (against staging)
  └── k6 performance tests (NFR-12 broadcast validation)
```

---

## 9. What Must Not Be Mocked

The following must never be replaced with mocks in any test type:

| Component | Reason |
|---|---|
| `AclsDbContext` in integration tests | Mocking EF Core does not test SQL generation, indexes, or transactions |
| Domain entity state machines | Use real entity methods — mocking would defeat the test's purpose |
| `TransactionBehaviour` in transaction tests | The whole point is testing EF Core transaction rollback |

The following should always be mocked in unit tests:

| Component | Mock with |
|---|---|
| `IComplaintRepository` | NSubstitute |
| `IStaffRepository` | NSubstitute |
| `INotificationService` | NSubstitute |
| `IStorageService` | NSubstitute |
| `ICurrentPropertyContext` | NSubstitute (return `PropertyId = 1`) |
| `IPublisher` (MediatR) | NSubstitute |

---

## 10. Checklist — Before Committing Any Test

- [ ] Test class uses `[TestFixture]`, not `[TestClass]` or no attribute.
- [ ] Test methods use `[Test]`, not `[Fact]`, `[TestMethod]`, or no attribute.
- [ ] Test name follows `<Method>_<Scenario>_<ExpectedOutcome>` convention.
- [ ] Three-section Arrange/Act/Assert structure present with blank line separators.
- [ ] One assertion concept per test method.
- [ ] For integration tests: does the test include a cross-property isolation assertion?
- [ ] For transaction tests: does the test verify rollback behaviour?
- [ ] No hardcoded integers or strings inline — use `TestDataFactory` constants or well-named variables.
- [ ] Test is in the correct `tests/<type>/` directory, not inside an application project.

---

*End of Testing Strategy v1.0*
