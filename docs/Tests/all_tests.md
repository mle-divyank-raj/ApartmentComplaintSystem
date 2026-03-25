# ACLS Test Catalogue

All test cases organised by domain. Each entry includes the test level, the full file path,
and the exact function name. Test names follow the `Method_Scenario_ExpectedOutcome` convention.

**Total: 77 tests** across 13 test classes.

| Level | Count |
|---|---|
| Unit — Domain | 27 |
| Unit — Application | 22 |
| Integration | 21 |
| E2E | 7 |

---

## 1. Complaint

### 1.1 Domain — State Machine (Unit)

**File:** `tests/unit/Domain.Tests/Complaints/ComplaintTests.cs`  
**Class:** `ComplaintTests`

Tests the `Complaint` aggregate root: factory invariants, all valid and invalid state
machine transitions, domain event emission, and media attachment limits.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `Create_WithValidInputs_SetsStatusToOpen` | New complaint always starts in `OPEN` status |
| 2 | `Create_WithValidInputs_SetsCreatedAtToUtcNow` | `CreatedAt` is populated and in UTC |
| 3 | `Assign_WhenStatusIsOpen_SetsStatusToAssigned` | Happy-path: `OPEN → ASSIGNED` transition succeeds |
| 4 | `Assign_WhenStatusIsOpen_SetsAssignedStaffMemberId` | `AssignedStaffMemberId` is stored on assignment |
| 5 | `Assign_WhenStatusIsOpen_RaisesComplaintAssignedEvent` | `ComplaintAssignedEvent` is raised with correct staff ID |
| 6 | `Assign_WhenStatusIsClosed_ReturnsFailureResult` | Cannot re-assign a `CLOSED` complaint |
| 7 | `Assign_WhenStatusIsResolved_ReturnsFailureResult` | Cannot re-assign a `RESOLVED` complaint |
| 8 | `Resolve_WhenStatusIsInProgress_SetsStatusToResolved` | Happy-path: `IN_PROGRESS → RESOLVED` transition succeeds |
| 9 | `Resolve_WhenStatusIsInProgress_SetsResolvedAt` | `ResolvedAt` is populated and in UTC |
| 10 | `Resolve_WhenStatusIsInProgress_RaisesComplaintResolvedEvent` | `ComplaintResolvedEvent` is raised after resolution |
| 11 | `Resolve_WhenStatusIsOpen_ReturnsFailureResult` | Cannot resolve an `OPEN` complaint (transition invalid) |
| 12 | `Close_WhenStatusIsResolved_SetsStatusToClosed` | Happy-path: `RESOLVED → CLOSED` transition succeeds |
| 13 | `Close_WhenStatusIsOpen_ReturnsFailureResult` | Cannot close an `OPEN` complaint (transition invalid) |
| 14 | `AddMedia_WhenUnderLimit_AddsMedia` | Media attachment added when under `ComplaintConstants.MaxMediaAttachments` |
| 15 | `AddMedia_WhenAtLimit_ReturnsFailureResult` | Returns `Complaint.MaxMediaAttachmentsExceeded` when limit reached |

---

### 1.2 Application — Submit Complaint Handler (Unit)

**File:** `tests/unit/Application.Tests/Complaints/SubmitComplaintCommandHandlerTests.cs`  
**Class:** `SubmitComplaintCommandHandlerTests`

Tests `SubmitComplaintCommandHandler` with mocked `IComplaintRepository`,
`ICurrentPropertyContext`, and `IPublisher`. Verifies the handler does **not** call
`IStorageService` directly (media is uploaded by the controller before the command is built).

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `Handle_WithValidInputs_CreatesComplaintWithOpenStatus` | New complaint is created with `OPEN` status |
| 2 | `Handle_WithMediaUrls_CreatesMediaEntitiesWithUrlsOnly` | Pre-uploaded URLs are persisted as `Media` entities |
| 3 | `Handle_WithMediaUrls_DoesNotCallStorageService` | Handler never touches `IStorageService` |
| 4 | `Handle_WithValidInputs_SetsPropertyIdFromContext` | `PropertyId` is sourced from `ICurrentPropertyContext` |
| 5 | `Handle_WithValidInputs_RaisesComplaintSubmittedEvent` | `ComplaintSubmittedEvent` is published via `IPublisher` |

---

### 1.3 Application — Assign Complaint Handler (Unit)

**File:** `tests/unit/Application.Tests/Complaints/AssignComplaintCommandHandlerTests.cs`  
**Class:** `AssignComplaintCommandHandlerTests`

Tests `AssignComplaintCommandHandler` with mocked repositories and publisher.
Covers success path, not-found cases, state mutation, and cross-property isolation.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `Handle_WhenComplaintAndStaffExist_ReturnsSuccess` | Happy path: both entities found, result is success |
| 2 | `Handle_WhenComplaintNotFound_ReturnsNotFoundFailure` | Returns `Complaint.NotFound` when complaint is missing |
| 3 | `Handle_WhenStaffNotFound_ReturnsNotFoundFailure` | Returns `StaffMember.NotFound` when staff is missing |
| 4 | `Handle_WhenComplaintExists_SetsComplaintStatusToAssigned` | Complaint status transitions to `ASSIGNED` |
| 5 | `Handle_WhenComplaintExists_SetsStaffAvailabilityToBusy` | Staff availability transitions to `BUSY` |
| 6 | `Handle_WhenComplaintExists_PublishesDomainEvent` | `ComplaintAssignedEvent` is published via `IPublisher` |
| 7 | `Handle_WhenComplaintBelongsToDifferentProperty_ReturnsNotFound` | Cross-property complaint access returns `Complaint.NotFound` |

---

### 1.4 Application — Resolve Complaint Handler (Unit)

**File:** `tests/unit/Application.Tests/Complaints/ResolveComplaintCommandHandlerTests.cs`  
**Class:** `ResolveComplaintCommandHandlerTests`

Tests `ResolveComplaintCommandHandler`. Covers the happy path, staff side-effects,
timestamp setting, event publishing, and invalid-transition guard cases.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `Handle_WhenComplaintIsInProgress_ReturnsSuccess` | Happy path: `IN_PROGRESS` complaint resolves cleanly |
| 2 | `Handle_WhenComplaintIsInProgress_SetsStatusToResolved` | Status transitions to `RESOLVED` |
| 3 | `Handle_WhenComplaintIsInProgress_SetsStaffAvailabilityToAvailable` | Staff flips from `BUSY` → `AVAILABLE` |
| 4 | `Handle_WhenComplaintIsInProgress_SetsResolvedAt` | `ResolvedAt` is set and close to `UtcNow` |
| 5 | `Handle_WhenComplaintIsInProgress_PublishesComplaintResolvedEvent` | `ComplaintResolvedEvent` is published via `IPublisher` |
| 6 | `Handle_WhenComplaintIsOpen_ReturnsInvalidTransitionFailure` | Returns `Complaint.InvalidStatusTransition` for `OPEN` complaint |
| 7 | `Handle_WhenComplaintIsAlreadyResolved_ReturnsInvalidTransitionFailure` | Returns `Complaint.InvalidStatusTransition` when already `RESOLVED` |

---

### 1.5 Integration — Complaint Repository (Integration)

**File:** `tests/integration/Persistence.Tests/Repositories/ComplaintRepositoryTests.cs`  
**Class:** `ComplaintRepositoryTests`

Runs against a real SQL Server (TestContainers). Verifies persistence, querying,
filtering, pagination, and **PropertyId multi-tenancy isolation** for all repository methods.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `GetByIdAsync_WhenComplaintExists_ReturnsComplaint` | Persisted complaint is retrieved with correct field values |
| 2 | `GetByIdAsync_WhenComplaintNotFound_ReturnsNull` | Returns `null` for a non-existent complaint ID |
| 3 | `GetByIdAsync_WhenComplaintBelongsToDifferentProperty_ReturnsNull` | **Isolation:** complaint from Property 2 is invisible to Property 1 |
| 4 | `GetAllAsync_WithPropertyFilter_ReturnsOnlyPropertyComplaints` | **Isolation:** `GetAll` only returns complaints from the querying property |
| 5 | `GetAllAsync_WithStatusFilter_ReturnsFilteredComplaints` | Status filter on `GetAll` returns only matching complaints |
| 6 | `GetAllAsync_WithDateRangeFilter_ReturnsFilteredComplaints` | Date range filter includes and excludes correctly |
| 7 | `GetAllAsync_WithPagination_ReturnsCorrectPage` | Page 2, size 2 of 5 complaints returns exactly 2 results |
| 8 | `AddAsync_WithValidComplaint_PersistsToDatabase` | Complaint written via `AddAsync` is readable via a fresh context |
| 9 | `UpdateAsync_WhenComplaintStatusChanged_PersistsNewStatus` | Status change via `UpdateAsync` is durable on a fresh context read |

---

### 1.6 Integration — Assign Complaint Transaction (Integration)

**File:** `tests/integration/Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs`  
**Class:** `AssignComplaintTransactionTests`

Verifies that the assign operation is **atomic**: complaint status and staff availability
must either both commit or both roll back. Uses raw `BeginTransactionAsync` / `RollbackAsync`
to mirror the `TransactionBehaviour` MediatR pipeline.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `AssignComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | Commit: complaint is `ASSIGNED`, staff is `BUSY` after `CommitAsync` |
| 2 | `AssignComplaint_WhenStaffUpdateFails_ComplaintStatusNotChanged` | Rollback: complaint stays `OPEN`, staff stays `AVAILABLE` after simulated failure |

---

### 1.7 Integration — Resolve Complaint Transaction (Integration)

**File:** `tests/integration/Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs`  
**Class:** `ResolveComplaintTransactionTests`

Seeds a complaint in `IN_PROGRESS` state and verifies the resolve operation is atomic.
Seeds the full state machine chain (`OPEN → ASSIGNED → EN_ROUTE → IN_PROGRESS`) in the helper.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `ResolveComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | Commit: complaint is `RESOLVED` with `ResolvedAt` set, staff is `AVAILABLE` |
| 2 | `ResolveComplaint_WhenComplaintUpdateFails_StaffAvailabilityNotChanged` | Rollback: complaint stays `IN_PROGRESS`, `ResolvedAt` is null, staff stays `BUSY` |

---

### 1.8 E2E — Complaint Lifecycle (E2E)

**File:** `tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs`  
**Class:** `ComplaintLifecycleTests`

Full HTTP lifecycle tests using `WebApplicationFactory<Program>` with a real SQL Server
container. Infrastructure services (`IStorageService`, `INotificationService`) are replaced
with NSubstitute fakes.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `FullLifecycle_SubmitAssignResolveAndFeedback_CompletesSuccessfully` | `POST /complaints` → `POST /complaints/{id}/assign` → `GET /complaints/{id}` verifies `ASSIGNED` + staff `BUSY` |
| 2 | `SosLifecycle_TriggerSos_ComplaintCreatedWithSosStatus` | `POST /complaints/sos` returns 201 and GET verifies `SOS_EMERGENCY` urgency |
| 3 | `GetComplaintById_WhenComplaintExists_Returns200WithCorrectData` | `GET /complaints/{id}` returns 200 with correct `title`, `status`, and `urgency` |

---

## 2. Staff & Dispatch

### 2.1 Domain — Dispatch Algorithm (Unit)

**File:** `tests/unit/Domain.Tests/Dispatch/DispatchServiceTests.cs`  
**Class:** `DispatchServiceTests`

Tests the `DispatchService` scoring algorithm: `matchScore = (skillScore × 0.6 + idleScore × 0.4) × urgencyMultiplier`.
`IStaffRepository` is mocked — no database. Covers all 12 test cases specified in
`docs/07_Implementation/patterns/dispatch_algorithm.md Section 9`.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `FindOptimalStaff_WhenOneStaffHasAllRequiredSkills_RanksThatStaffFirst` | Full-skill-match staff outranks partial-match even with more idle time |
| 2 | `FindOptimalStaff_WhenStaffHasHalfRequiredSkills_SkillScoreIsPointFive` | 1 of 2 required skills → `skillScore = 0.5` |
| 3 | `FindOptimalStaff_WhenComplaintHasNoRequiredSkills_AllCandidatesGetSkillScoreOfOne` | No required skills → all candidates receive `skillScore = 1.0` |
| 4 | `FindOptimalStaff_WhenSkillScoresEqual_LongerIdleStaffRanksFirst` | On tied skill score, the more idle staff ranks higher |
| 5 | `FindOptimalStaff_WhenStaffHasNeverBeenAssigned_GetsMaxIdleScore` | `LastAssignedAt = null` → `idleScore = 1.0` (treated as infinitely idle) |
| 6 | `FindOptimalStaff_WhenUrgencyIsSosEmergency_MatchScoreIsDoubled` | `SOS_EMERGENCY` urgency multiplier is exactly `2.0 × HIGH` score |
| 7 | `FindOptimalStaff_WhenUrgencyIsSosEmergency_RankingOrderIsPreserved` | SOS multiplier scales all scores equally; relative ranking unchanged |
| 8 | `FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList` | Empty staff pool returns empty list without throwing |
| 9 | `FindOptimalStaff_WhenOneCandidateInPool_ReturnsSingleScoreWithIdleScoreOfOne` | Single candidate always normalised to `idleScore = 1.0` |
| 10 | `FindOptimalStaff_WhenAllCandidatesHaveSameLastAssignedAt_AllGetIdleScoreOfOne` | `maxIdleTime - minIdleTime = 0` → all candidates get `idleScore = 1.0` |
| 11 | `FindOptimalStaff_WhenSkillCasingDiffers_StillMatchesCorrectly` | Skill comparison is case-insensitive (`"PLUMBING"` matches `"plumbing"`) |
| 12 | `FindOptimalStaff_WhenPerfectMatch_MatchScoreIsOne` | Perfect skill + idle score, `HIGH` urgency → `matchScore = 1.0` |

---

### 2.2 Integration — Staff Repository (Integration)

**File:** `tests/integration/Persistence.Tests/Repositories/StaffRepositoryTests.cs`  
**Class:** `StaffRepositoryTests`

`StaffMember` has no direct `PropertyId` column — scoping is enforced via a join through
`User.PropertyId`. These tests verify that join-based isolation is correctly applied.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `GetByIdAsync_WhenStaffExists_ReturnsStaffMember` | Happy path: staff in property is retrievable with correct fields |
| 2 | `GetByIdAsync_WhenStaffBelongsToDifferentProperty_ReturnsNull` | **Isolation:** staff from Property 2 is invisible to Property 1 via `GetById` |
| 3 | `GetAvailableAsync_ReturnsOnlyAvailableStaffForProperty` | Only `AVAILABLE` staff are returned; `BUSY` staff are excluded |
| 4 | `GetAvailableAsync_WhenStaffBelongsToDifferentProperty_ReturnsEmpty` | **Isolation:** available staff from another property are not returned |
| 5 | `UpdateAsync_WhenAvailabilityChanged_PersistsNewAvailability` | `MarkBusy()` is persisted; `LastAssignedAt` is populated after update |

---

## 3. Outage

### 3.1 Application — Declare Outage Handler (Unit)

**File:** `tests/unit/Application.Tests/Outages/DeclareOutageCommandHandlerTests.cs`  
**Class:** `DeclareOutageCommandHandlerTests`

Tests `DeclareOutageCommandHandler`. Verifies that outages are created, events fired, and
that the handler does **not** call `INotificationService` directly (notifications are
dispatched asynchronously by the worker via `OutageDeclaredEvent`).

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `Handle_WithValidInputs_CreatesOutageRecord` | `Outage` is added to the repository with correct `Title`, `OutageType`, and `PropertyId` |
| 2 | `Handle_WithValidInputs_PublishesOutageDeclaredEvent` | `OutageDeclaredEvent` is published via `IPublisher` |
| 3 | `Handle_WithValidInputs_DoesNotCallNotificationServiceDirectly` | Handler only calls repository once and publisher once — no `INotificationService` usage |

---

## 4. Audit Log

### 4.1 Integration — Audit Repository (Integration)

**File:** `tests/integration/Persistence.Tests/Repositories/AuditRepositoryTests.cs`  
**Class:** `AuditRepositoryTests`

Verifies the immutable append-only nature of `AuditRepository`. Only `AddAsync` is
exposed — there is no `Update` or `Delete`. All reads are done via a fresh context
to confirm persistence.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `AddAsync_WithValidEntry_PersistsToDatabase` | Entry is readable via fresh context with all fields correct, including UTC `OccurredAt` |
| 2 | `AddAsync_WithMultipleEntries_AllPersisted` | Three sequential entries are all stored and returned in insertion order |
| 3 | `AddAsync_WithSystemInitiatedEntry_PersistsWithNullPropertyAndActor` | System-initiated entry stores `null` for `PropertyId`, `ActorUserId`, and `ActorRole` |

---

## 5. Multi-Tenancy

Cross-cutting isolation tests verifying the core **multi-tenancy rule**: a user from
Property A must never read, assign, or act on resources belonging to Property B.
All cross-property requests must return `404 Not Found`.

> **Note:** PropertyId isolation is also tested as part of the Complaint and Staff domains
> above (sections 1.5, 1.8, 2.2). This section contains the dedicated HTTP-layer isolation test class.

### 5.1 E2E — HTTP Layer Isolation (E2E)

**File:** `tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs`  
**Class:** `MultiTenancyIsolationTests`

Uses real JWT tokens scoped to different `property_id` claims. Complaints and staff are seeded
directly into the DB under isolated properties. All cross-property HTTP calls are expected
to return `404`.

| # | Test Function | What It Verifies |
|---|---|---|
| 1 | `GetComplaint_WhenComplaintBelongsToDifferentProperty_Returns404` | `GET /complaints/{id}` returns 404 when the complaint's `PropertyId` differs from the JWT claim |
| 2 | `GetAllComplaints_ReturnsOnlyAuthenticatedPropertyComplaints` | `GET /complaints` returns only complaints for the authenticated property; cross-property complaint not included |
| 3 | `AssignComplaint_WhenStaffBelongsToDifferentProperty_Returns404` | `POST /complaints/{id}/assign` returns 404 when the target staff member belongs to a different property |

---

## Summary by Test Level

### Unit Tests — Domain (27 tests)

| Class | File | Count |
|---|---|---|
| `ComplaintTests` | `tests/unit/Domain.Tests/Complaints/ComplaintTests.cs` | 15 |
| `DispatchServiceTests` | `tests/unit/Domain.Tests/Dispatch/DispatchServiceTests.cs` | 12 |

### Unit Tests — Application (22 tests)

| Class | File | Count |
|---|---|---|
| `SubmitComplaintCommandHandlerTests` | `tests/unit/Application.Tests/Complaints/SubmitComplaintCommandHandlerTests.cs` | 5 |
| `AssignComplaintCommandHandlerTests` | `tests/unit/Application.Tests/Complaints/AssignComplaintCommandHandlerTests.cs` | 7 |
| `ResolveComplaintCommandHandlerTests` | `tests/unit/Application.Tests/Complaints/ResolveComplaintCommandHandlerTests.cs` | 7 |
| `DeclareOutageCommandHandlerTests` | `tests/unit/Application.Tests/Outages/DeclareOutageCommandHandlerTests.cs` | 3 |

### Integration Tests (21 tests)

| Class | File | Count |
|---|---|---|
| `ComplaintRepositoryTests` | `tests/integration/Persistence.Tests/Repositories/ComplaintRepositoryTests.cs` | 9 |
| `StaffRepositoryTests` | `tests/integration/Persistence.Tests/Repositories/StaffRepositoryTests.cs` | 5 |
| `AuditRepositoryTests` | `tests/integration/Persistence.Tests/Repositories/AuditRepositoryTests.cs` | 3 |
| `AssignComplaintTransactionTests` | `tests/integration/Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs` | 2 |
| `ResolveComplaintTransactionTests` | `tests/integration/Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs` | 2 |

### E2E Tests (7 tests)

| Class | File | Count |
|---|---|---|
| `ComplaintLifecycleTests` | `tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs` | 3 |
| `MultiTenancyIsolationTests` | `tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs` | 3 |

> **Note:** `E2ETestBase` and `IntegrationTestBase` are shared infrastructure classes, not test classes.
> `DomainTestExtensions` and `TestDataFactory` are helper classes. None of these contain `[Test]` methods.
