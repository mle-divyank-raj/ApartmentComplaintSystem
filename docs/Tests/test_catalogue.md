# Test Catalogue

**Document:** `docs/07_Implementation/test_catalogue.md`  
**Version:** 2.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)  
**Change from v1.0:** Merged with actual code-generated tests from Phases 1–4b. Added 3 tests found in code that were missing from spec (2 Staff repository, 1 Audit repository). Corrected E2E lifecycle test #3. Marked Infrastructure, Contract, and k6 tests as Phase 6+ (not yet written).

---

> [!IMPORTANT]
> This document is the single source of truth for every test in the ACLS project. Tests marked **✅ Written** exist in the codebase (Phases 1–4b). Tests marked **⏳ Phase 6+** are specified but not yet written — they are the acceptance criteria for Phase 6 and Phase 7. A test described here that does not exist in code is an incomplete feature.

---

## Summary Counts

| Domain | Unit | Integration | E2E | Contract | Total | Written |
|---|---|---|---|---|---|---|
| Complaints | 29 | 13 | 3 | 1 | **46** | 45 written, 1 Phase 6+ |
| Dispatch | 12 | 0 | 0 | 0 | **12** | 12 written |
| Staff | 0 | 5 | 1 | 0 | **6** | 5 written, 1 in lifecycle |
| Outages | 3 | 0 | 1 | 0 | **4** | 3 written, 1 Phase 6+ |
| Multi-Tenancy | 0 | 0 | 3 | 0 | **3** | 3 written |
| Audit | 0 | 3 | 0 | 0 | **3** | 3 written |
| Infrastructure | 0 | 3 | 0 | 0 | **3** | ⏳ Phase 6+ |
| Performance | — | — | — | — | **1** | ⏳ Phase 7+ |
| **Total** | **44** | **24** | **8** | **1** | **77** | **71 written** |

---

---

# 1. Complaints Domain

---

## 1.1 Complaint Entity — State Machine (Unit) ✅ Written

**Class:** `ComplaintTests`  
**File:** `tests/unit/Domain.Tests/Complaints/ComplaintTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Domain.Complaints.Complaint`

| # | Test method | Verifies |
|---|---|---|
| 1 | `Create_WithValidInputs_SetsStatusToOpen` | New complaint always starts with `Status = OPEN` |
| 2 | `Create_WithValidInputs_SetsCreatedAtToUtcNow` | `CreatedAt` is populated with UTC time at creation |
| 3 | `Assign_WhenStatusIsOpen_SetsStatusToAssigned` | Valid `OPEN → ASSIGNED` transition sets status correctly |
| 4 | `Assign_WhenStatusIsOpen_SetsAssignedStaffMemberId` | `AssignedStaffMemberId` FK is populated on assignment |
| 5 | `Assign_WhenStatusIsOpen_RaisesComplaintAssignedEvent` | `ComplaintAssignedEvent` is raised with correct staff ID |
| 6 | `Assign_WhenStatusIsClosed_ReturnsFailureResult` | `CLOSED` complaint cannot be assigned — returns `Complaint.InvalidStatusTransition` |
| 7 | `Assign_WhenStatusIsResolved_ReturnsFailureResult` | `RESOLVED` complaint cannot be assigned — returns `Complaint.InvalidStatusTransition` |
| 8 | `Resolve_WhenStatusIsInProgress_SetsStatusToResolved` | Valid `IN_PROGRESS → RESOLVED` transition sets status correctly |
| 9 | `Resolve_WhenStatusIsInProgress_SetsResolvedAt` | `ResolvedAt` is populated with UTC time on resolution |
| 10 | `Resolve_WhenStatusIsInProgress_RaisesComplaintResolvedEvent` | `ComplaintResolvedEvent` is raised after resolution |
| 11 | `Resolve_WhenStatusIsOpen_ReturnsFailureResult` | `OPEN` complaint cannot be resolved — returns `Complaint.InvalidStatusTransition` |
| 12 | `Close_WhenStatusIsResolved_SetsStatusToClosed` | Valid `RESOLVED → CLOSED` transition sets status correctly |
| 13 | `Close_WhenStatusIsOpen_ReturnsFailureResult` | `OPEN` complaint cannot be closed — returns `Complaint.InvalidStatusTransition` |
| 14 | `AddMedia_WhenUnderLimit_AddsMedia` | `Media` entity added when count is below `ComplaintConstants.MaxMediaAttachments` (3) |
| 15 | `AddMedia_WhenAtLimit_ReturnsFailureResult` | Adding a 4th `Media` entity returns `Complaint.MaxMediaAttachmentsExceeded` |

---

## 1.2 Submit Complaint Handler (Unit) ✅ Written

**Class:** `SubmitComplaintCommandHandlerTests`  
**File:** `tests/unit/Application.Tests/Complaints/SubmitComplaintCommandHandlerTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Application.Complaints.Commands.SubmitComplaint.SubmitComplaintCommandHandler`  
**Mocks:** `IComplaintRepository`, `ICurrentPropertyContext`, `IPublisher`  
**Key constraint verified:** Handler never calls `IStorageService` — blob upload is the controller's responsibility

| # | Test method | Verifies |
|---|---|---|
| 1 | `Handle_WithValidInputs_CreatesComplaintWithOpenStatus` | New complaint created with `Status = OPEN` |
| 2 | `Handle_WithMediaUrls_CreatesMediaEntitiesWithUrlsOnly` | Pre-uploaded URL strings are persisted as `Media` entities — no binary content |
| 3 | `Handle_WithMediaUrls_DoesNotCallStorageService` | `IStorageService` is never called inside the handler |
| 4 | `Handle_WithValidInputs_SetsPropertyIdFromContext` | `PropertyId` sourced from `ICurrentPropertyContext` — never from request fields |
| 5 | `Handle_WithValidInputs_RaisesComplaintSubmittedEvent` | `ComplaintSubmittedEvent` published via `IPublisher` after persistence |

---

## 1.3 Assign Complaint Handler (Unit) ✅ Written

**Class:** `AssignComplaintCommandHandlerTests`  
**File:** `tests/unit/Application.Tests/Complaints/AssignComplaintCommandHandlerTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Application.Complaints.Commands.AssignComplaint.AssignComplaintCommandHandler`  
**Mocks:** `IComplaintRepository`, `IStaffRepository`, `ICurrentPropertyContext`, `IPublisher`

| # | Test method | Verifies |
|---|---|---|
| 1 | `Handle_WhenComplaintAndStaffExist_ReturnsSuccess` | Happy path — returns `Result.Success(ComplaintDto)` |
| 2 | `Handle_WhenComplaintNotFound_ReturnsNotFoundFailure` | Returns `Complaint.NotFound` when complaint is missing |
| 3 | `Handle_WhenStaffNotFound_ReturnsNotFoundFailure` | Returns `Staff.NotFound` when staff member is missing |
| 4 | `Handle_WhenComplaintExists_SetsComplaintStatusToAssigned` | `Complaint.Status` transitions to `ASSIGNED` |
| 5 | `Handle_WhenComplaintExists_SetsStaffAvailabilityToBusy` | `StaffMember.Availability` transitions to `BUSY` |
| 6 | `Handle_WhenComplaintExists_PublishesDomainEvent` | `ComplaintAssignedEvent` published via `IPublisher` |
| 7 | `Handle_WhenComplaintBelongsToDifferentProperty_ReturnsNotFound` | Cross-property complaint access returns `Complaint.NotFound` (not 403) |

---

## 1.4 Resolve Complaint Handler (Unit) ✅ Written

**Class:** `ResolveComplaintCommandHandlerTests`  
**File:** `tests/unit/Application.Tests/Complaints/ResolveComplaintCommandHandlerTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Application.Complaints.Commands.ResolveComplaint.ResolveComplaintCommandHandler`  
**Mocks:** `IComplaintRepository`, `IStaffRepository`, `ICurrentPropertyContext`, `IPublisher`

| # | Test method | Verifies |
|---|---|---|
| 1 | `Handle_WhenComplaintIsInProgress_ReturnsSuccess` | Happy path — returns `Result.Success(ComplaintDto)` |
| 2 | `Handle_WhenComplaintIsInProgress_SetsStatusToResolved` | `Complaint.Status` transitions to `RESOLVED` |
| 3 | `Handle_WhenComplaintIsInProgress_SetsStaffAvailabilityToAvailable` | `StaffMember.Availability` transitions from `BUSY` to `AVAILABLE` |
| 4 | `Handle_WhenComplaintIsInProgress_SetsResolvedAt` | `Complaint.ResolvedAt` set to UTC now |
| 5 | `Handle_WhenComplaintIsInProgress_PublishesComplaintResolvedEvent` | `ComplaintResolvedEvent` published via `IPublisher` |
| 6 | `Handle_WhenComplaintIsOpen_ReturnsInvalidTransitionFailure` | Returns `Complaint.InvalidStatusTransition` for `OPEN` complaint |
| 7 | `Handle_WhenComplaintIsAlreadyResolved_ReturnsInvalidTransitionFailure` | Returns `Complaint.InvalidStatusTransition` when already `RESOLVED` |

---

## 1.5 Complaint Repository (Integration) ✅ Written

**Class:** `ComplaintRepositoryTests`  
**File:** `tests/integration/Persistence.Tests/Repositories/ComplaintRepositoryTests.cs`  
**Test type:** Integration — TestContainers SQL Server + Respawn  
**Class under test:** `ACLS.Persistence.Repositories.ComplaintRepository`

| # | Test method | Verifies |
|---|---|---|
| 1 | `GetByIdAsync_WhenComplaintExists_ReturnsComplaint` | Persisted complaint retrieved with correct field values |
| 2 | `GetByIdAsync_WhenComplaintNotFound_ReturnsNull` | Returns `null` (not exception) for non-existent ID |
| 3 | `GetByIdAsync_WhenComplaintBelongsToDifferentProperty_ReturnsNull` | **Multi-tenancy:** Property 2 complaint invisible to Property 1 |
| 4 | `GetAllAsync_WithPropertyFilter_ReturnsOnlyPropertyComplaints` | **Multi-tenancy:** List query never includes cross-property rows |
| 5 | `GetAllAsync_WithStatusFilter_ReturnsFilteredComplaints` | Status filter correctly restricts results |
| 6 | `GetAllAsync_WithDateRangeFilter_ReturnsFilteredComplaints` | Date range filter correctly includes and excludes by `CreatedAt` |
| 7 | `GetAllAsync_WithPagination_ReturnsCorrectPage` | `Page`/`PageSize` returns the correct window of results |
| 8 | `AddAsync_WithValidComplaint_PersistsToDatabase` | Entity written via `AddAsync` is readable on a fresh context |
| 9 | `UpdateAsync_WhenComplaintStatusChanged_PersistsNewStatus` | Status change via `UpdateAsync` is durable on fresh context read |

---

## 1.6 Assign Complaint — Transaction Atomicity (Integration) ✅ Written

**Class:** `AssignComplaintTransactionTests`  
**File:** `tests/integration/Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs`  
**Test type:** Integration — TestContainers SQL Server + Respawn  
**Key constraint verified:** Both complaint and staff writes commit or both roll back — never partial

| # | Test method | Verifies |
|---|---|---|
| 1 | `AssignComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | After commit: `Complaint.Status = ASSIGNED` and `StaffMember.Availability = BUSY` both persisted |
| 2 | `AssignComplaint_WhenStaffUpdateFails_ComplaintStatusNotChanged` | **Rollback:** complaint stays `OPEN`, staff stays `AVAILABLE` after simulated failure |

---

## 1.7 Resolve Complaint — Transaction Atomicity (Integration) ✅ Written

**Class:** `ResolveComplaintTransactionTests`  
**File:** `tests/integration/Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs`  
**Test type:** Integration — TestContainers SQL Server + Respawn

| # | Test method | Verifies |
|---|---|---|
| 1 | `ResolveComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction` | After commit: `Complaint.Status = RESOLVED` with `ResolvedAt` set, `StaffMember.Availability = AVAILABLE` |
| 2 | `ResolveComplaint_WhenComplaintUpdateFails_StaffAvailabilityNotChanged` | **Rollback:** complaint stays `IN_PROGRESS`, `ResolvedAt` is null, staff stays `BUSY` |

---

## 1.8 Complaint Lifecycle — E2E ✅ Written

**Class:** `ComplaintLifecycleTests`  
**File:** `tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs`  
**Test type:** E2E — `WebApplicationFactory<Program>` + TestContainers SQL Server  
**Note:** `IStorageService` and `INotificationService` replaced with NSubstitute fakes

| # | Test method | Verifies |
|---|---|---|
| 1 | `FullLifecycle_SubmitAssignResolveAndFeedback_CompletesSuccessfully` | Full HTTP lifecycle: `POST /complaints` → assign → status updates → resolve → feedback. Final status = `CLOSED` |
| 2 | `SosLifecycle_TriggerSos_ComplaintCreatedWithSosStatus` | `POST /complaints/sos` returns 201 and `GET /complaints/{id}` confirms `urgency = SOS_EMERGENCY` |
| 3 | `GetComplaintById_WhenComplaintExists_Returns200WithCorrectData` | `GET /complaints/{id}` returns 200 with correct `title`, `status`, and `urgency` fields |

> **Note:** An outage E2E test (`OutageLifecycle_DeclareOutage_OutageCreatedAndBroadcastQueued`) was specified but not yet written. It should be added in Phase 6 to this file.

---

## 1.9 Complaints — Contract Test ⏳ Phase 6+

**Class:** `ComplaintsContractTests`  
**File:** `tests/contract/Api.ContractTests/ComplaintsContractTests.cs`  
**Test type:** Contract — `WebApplicationFactory<Program>`

| # | Test method | Verifies |
|---|---|---|
| 1 | `SubmitComplaint_ResponseShape_MatchesV1YamlComplaintDtoSchema` | `POST /complaints` response deserialises to valid `ComplaintDto` with all required fields, correct types, camelCase names, ISO 8601 UTC datetimes, SCREAMING_SNAKE enum values |

---

---

# 2. Dispatch Domain

---

## 2.1 Dispatch Service — Algorithm (Unit) ✅ Written

**Class:** `DispatchServiceTests`  
**File:** `tests/unit/Domain.Tests/Dispatch/DispatchServiceTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Infrastructure.Dispatch.DispatchService`  
**Formula verified:** `matchScore = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight`

| # | Test method | Verifies |
|---|---|---|
| 1 | `FindOptimalStaff_WhenOneStaffHasAllRequiredSkills_RanksThatStaffFirst` | Full skill match outranks partial match even with more idle time |
| 2 | `FindOptimalStaff_WhenStaffHasHalfRequiredSkills_SkillScoreIsPointFive` | 1 of 2 required skills → `SkillScore = 0.5` |
| 3 | `FindOptimalStaff_WhenComplaintHasNoRequiredSkills_AllCandidatesGetSkillScoreOfOne` | Empty `RequiredSkills` → all candidates get `SkillScore = 1.0` |
| 4 | `FindOptimalStaff_WhenSkillScoresEqual_LongerIdleStaffRanksFirst` | Tied skill scores → more idle candidate ranks first |
| 5 | `FindOptimalStaff_WhenStaffHasNeverBeenAssigned_GetsMaxIdleScore` | `LastAssignedAt = null` → `IdleScore = 1.0` |
| 6 | `FindOptimalStaff_WhenUrgencyIsSosEmergency_MatchScoreIsDoubled` | `SOS_EMERGENCY` `urgencyWeight = 2.0` → final score is exactly double `HIGH` urgency |
| 7 | `FindOptimalStaff_WhenUrgencyIsSosEmergency_RankingOrderIsPreserved` | SOS multiplier scales all scores equally — relative ranking unchanged |
| 8 | `FindOptimalStaff_WhenNoStaffAvailable_ReturnsEmptyList` | Empty staff pool → empty list returned, no exception thrown |
| 9 | `FindOptimalStaff_WhenOneCandidateInPool_ReturnsSingleScoreWithIdleScoreOfOne` | Single candidate always normalised to `IdleScore = 1.0` |
| 10 | `FindOptimalStaff_WhenAllCandidatesHaveSameLastAssignedAt_AllGetIdleScoreOfOne` | Zero max idle time → all candidates get `IdleScore = 1.0`, division by zero handled |
| 11 | `FindOptimalStaff_WhenSkillCasingDiffers_StillMatchesCorrectly` | Skill matching is case-insensitive: `"PLUMBING"` matches `"plumbing"` |
| 12 | `FindOptimalStaff_WhenPerfectMatch_MatchScoreIsOne` | `SkillScore = 1.0`, `IdleScore = 1.0`, `urgencyWeight = 1.0` → `MatchScore = 1.0` exactly |

---

---

# 3. Staff Domain

---

## 3.1 Staff Repository (Integration) ✅ Written

**Class:** `StaffRepositoryTests`  
**File:** `tests/integration/Persistence.Tests/Repositories/StaffRepositoryTests.cs`  
**Test type:** Integration — TestContainers SQL Server + Respawn  
**Class under test:** `ACLS.Persistence.Repositories.StaffRepository`  
**Note:** `StaffMember` has no direct `PropertyId` column — scoping enforced via join through `User.PropertyId`. Tests verify this join-based isolation is correctly applied.

| # | Test method | Verifies |
|---|---|---|
| 1 | `GetByIdAsync_WhenStaffExists_ReturnsStaffMember` | Staff member in the property is retrievable with correct fields |
| 2 | `GetByIdAsync_WhenStaffBelongsToDifferentProperty_ReturnsNull` | **Multi-tenancy:** Staff from Property 2 is invisible to Property 1 via `GetById` |
| 3 | `GetAvailableAsync_ReturnsOnlyAvailableStaffForProperty` | Only `AVAILABLE` staff returned; `BUSY`/`ON_BREAK`/`OFF_DUTY` excluded |
| 4 | `GetAvailableAsync_WhenStaffBelongsToDifferentProperty_ReturnsEmpty` | **Multi-tenancy:** Available staff from another property are not returned |
| 5 | `UpdateAsync_WhenAvailabilityChanged_PersistsNewAvailability` | `MarkBusy()` is durable; `LastAssignedAt` is populated after update |

> **Tests 1, 2, and 5 were not in the v1.0 spec — they were found in the actual code written during Phase 1. The spec only required the two `GetAvailableAsync` tests. The code is more thorough and these 3 tests are now part of the authoritative catalogue.**

---

---

# 4. Outages Domain

---

## 4.1 Declare Outage Handler (Unit) ✅ Written

**Class:** `DeclareOutageCommandHandlerTests`  
**File:** `tests/unit/Application.Tests/Outages/DeclareOutageCommandHandlerTests.cs`  
**Test type:** Unit  
**Class under test:** `ACLS.Application.Outages.Commands.DeclareOutage.DeclareOutageCommandHandler`  
**Mocks:** `IOutageRepository`, `IPublisher`  
**Key constraint verified:** Handler never calls `INotificationService` directly

| # | Test method | Verifies |
|---|---|---|
| 1 | `Handle_WithValidInputs_CreatesOutageRecord` | `Outage` persisted with correct `Title`, `OutageType`, and `PropertyId` |
| 2 | `Handle_WithValidInputs_PublishesOutageDeclaredEvent` | `OutageDeclaredEvent` published via `IPublisher` |
| 3 | `Handle_WithValidInputs_DoesNotCallNotificationServiceDirectly` | `INotificationService` never called — notification is async via Worker |

---

## 4.2 Outage Lifecycle — E2E ⏳ Phase 6+

**Class:** `ComplaintLifecycleTests`  
**File:** `tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs`  
**Test type:** E2E

| # | Test method | Verifies |
|---|---|---|
| 1 | `OutageLifecycle_DeclareOutage_OutageCreatedAndBroadcastQueued` | `POST /outages` creates a persisted `Outage` with `notificationSentAt = null` and publishes `OutageDeclaredEvent` without blocking the HTTP response |

---

---

# 5. Multi-Tenancy (Cross-Domain)

---

## 5.1 Multi-Tenancy Isolation — E2E ✅ Written

**Class:** `MultiTenancyIsolationTests`  
**File:** `tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs`  
**Test type:** E2E — real JWT tokens with different `property_id` claims  
**Note:** Multi-tenancy is also tested at the repository level in Sections 1.5 (#3, #4), 3.1 (#2, #4)

| # | Test method | Verifies |
|---|---|---|
| 1 | `GetComplaint_WhenComplaintBelongsToDifferentProperty_Returns404` | `GET /complaints/{id}` returns `404` when complaint's `PropertyId` ≠ JWT claim's `property_id`. Not `403` — prevents ID enumeration. |
| 2 | `GetAllComplaints_ReturnsOnlyAuthenticatedPropertyComplaints` | `GET /complaints` never returns cross-property rows even when they exist in the same database |
| 3 | `AssignComplaint_WhenStaffBelongsToDifferentProperty_Returns404` | `POST /complaints/{id}/assign` returns `404` when the target staff member belongs to a different property |

---

---

# 6. Audit Domain

---

## 6.1 Audit Repository (Integration) ✅ Written

**Class:** `AuditRepositoryTests`  
**File:** `tests/integration/Persistence.Tests/Repositories/AuditRepositoryTests.cs`  
**Test type:** Integration — TestContainers SQL Server + Respawn  
**Class under test:** `ACLS.Persistence.Repositories.AuditRepository`  
**Key constraint verified:** `IAuditRepository` exposes `AddAsync` only — no update or delete

| # | Test method | Verifies |
|---|---|---|
| 1 | `AddAsync_WithValidEntry_PersistsToDatabase` | Entry readable via fresh context with all fields correct, including UTC `OccurredAt` |
| 2 | `AddAsync_WithMultipleEntries_AllPersisted` | Three sequential entries are all stored and returned in insertion order |
| 3 | `AddAsync_WithSystemInitiatedEntry_PersistsWithNullPropertyAndActor` | System-initiated entry (Worker job) stores `null` for `PropertyId`, `ActorUserId`, and `ActorRole` — nullable FK design works correctly |

> **Test #3 was not in the v1.0 spec — it was found in the actual code. It covers the important case where background jobs write audit entries without a user context. Now part of the authoritative catalogue.**

---

---

# 7. Infrastructure Domain

---

## 7.1 Azure Blob Storage Service (Integration) ⏳ Phase 6+

**Class:** `AzureBlobStorageServiceTests`  
**File:** `tests/integration/Infrastructure.Tests/Storage/AzureBlobStorageServiceTests.cs`  
**Test type:** Integration — Azurite Docker emulator  
**Class under test:** `ACLS.Infrastructure.Storage.AzureBlobStorageService`  
**Prerequisite:** Azurite running via `docker-compose up`

| # | Test method | Verifies |
|---|---|---|
| 1 | `UploadAsync_WithValidJpegStream_ReturnsNonEmptyUrl` | Valid JPEG stream upload returns a non-empty blob URL string |
| 2 | `UploadAsync_WithValidJpegStream_BlobExistsInStorage` | After `UploadAsync`, the blob actually exists in the Azurite container |
| 3 | `UploadAsync_GeneratesUniqueBlobName_ForDuplicateFileName` | Two uploads with identical `fileName` produce different blob names — no collision |

---

---

# 8. Performance Tests

---

## 8.1 Outage Broadcast Load Test ⏳ Phase 7+

**File:** `tests/performance/k6/outage_broadcast.js`  
**Test type:** Performance — k6  
**When run:** On-demand / Release candidate only — not in standard CI  
**NFR reference:** NFR-12 (500 messages within 60 seconds)

| # | Scenario | Verifies |
|---|---|---|
| 1 | `baseline_500_residents_60_seconds` | Declares one outage for a property with 500 residents. Measures time from `OutageDeclaredEvent` to `Outage.NotificationSentAt` being set. Target: ≤ 60 seconds. |

---

---

# 9. Test File Index

| File | Path | Type | Domain | Status |
|---|---|---|---|---|
| `ComplaintTests.cs` | `tests/unit/Domain.Tests/Complaints/ComplaintTests.cs` | Unit | Complaints | ✅ Written |
| `DispatchServiceTests.cs` | `tests/unit/Domain.Tests/Dispatch/DispatchServiceTests.cs` | Unit | Dispatch | ✅ Written |
| `SubmitComplaintCommandHandlerTests.cs` | `tests/unit/Application.Tests/Complaints/SubmitComplaintCommandHandlerTests.cs` | Unit | Complaints | ✅ Written |
| `AssignComplaintCommandHandlerTests.cs` | `tests/unit/Application.Tests/Complaints/AssignComplaintCommandHandlerTests.cs` | Unit | Complaints | ✅ Written |
| `ResolveComplaintCommandHandlerTests.cs` | `tests/unit/Application.Tests/Complaints/ResolveComplaintCommandHandlerTests.cs` | Unit | Complaints | ✅ Written |
| `DeclareOutageCommandHandlerTests.cs` | `tests/unit/Application.Tests/Outages/DeclareOutageCommandHandlerTests.cs` | Unit | Outages | ✅ Written |
| `IntegrationTestBase.cs` | `tests/integration/Persistence.Tests/IntegrationTestBase.cs` | Base class | — | ✅ Written |
| `ComplaintRepositoryTests.cs` | `tests/integration/Persistence.Tests/Repositories/ComplaintRepositoryTests.cs` | Integration | Complaints | ✅ Written |
| `StaffRepositoryTests.cs` | `tests/integration/Persistence.Tests/Repositories/StaffRepositoryTests.cs` | Integration | Staff | ✅ Written |
| `AuditRepositoryTests.cs` | `tests/integration/Persistence.Tests/Repositories/AuditRepositoryTests.cs` | Integration | Audit | ✅ Written |
| `AssignComplaintTransactionTests.cs` | `tests/integration/Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs` | Integration | Complaints | ✅ Written |
| `ResolveComplaintTransactionTests.cs` | `tests/integration/Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs` | Integration | Complaints | ✅ Written |
| `AzureBlobStorageServiceTests.cs` | `tests/integration/Infrastructure.Tests/Storage/AzureBlobStorageServiceTests.cs` | Integration | Infrastructure | ⏳ Phase 6+ |
| `ComplaintsContractTests.cs` | `tests/contract/Api.ContractTests/ComplaintsContractTests.cs` | Contract | Complaints | ⏳ Phase 6+ |
| `ComplaintLifecycleTests.cs` | `tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs` | E2E | Complaints / Outages | ✅ 3 written, 1 ⏳ Phase 6+ |
| `MultiTenancyIsolationTests.cs` | `tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs` | E2E | Multi-Tenancy | ✅ Written |
| `DashboardTests.cs` | `tests/e2e/Web.E2ETests/DashboardTests.cs` | E2E (Playwright) | UI | ⏳ Phase 6+ |
| `outage_broadcast.js` | `tests/performance/k6/outage_broadcast.js` | Performance | Outages | ⏳ Phase 7+ |

---

# 10. CI Pipeline Schedule

| Suite | When it runs | Target time | Command |
|---|---|---|---|
| `Domain.Tests` + `Application.Tests` | Every commit, every PR | < 30 seconds | `dotnet test tests/unit/` |
| `Persistence.Tests` + `Infrastructure.Tests` | Every PR | < 3 minutes | `dotnet test tests/integration/` |
| `Api.ContractTests` | Every PR | < 1 minute | `dotnet test tests/contract/` |
| `Api.E2ETests` | Merge to `main` | < 5 minutes | `dotnet test tests/e2e/Api.E2ETests/` |
| `Web.E2ETests` (Playwright) | Release candidate, against staging | — | `npx playwright test` |
| k6 performance | On-demand / Release candidate | — | `k6 run tests/performance/k6/outage_broadcast.js` |

---

*End of Test Catalogue v2.0*
