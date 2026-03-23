# ACLS — Repository Blueprint

**Version:** 1.0  
**Date:** 2026-03-19  
**Status:** Draft — Pending Review  
**Project:** Apartment Complaint Logging System  
**Client:** Apartment Complex Association  
**Vendor:** UTD CS 3354

---

## 1. Repository Overview

ACLS is a multi-tenant SaaS platform that manages the full request-to-resolution lifecycle for apartment maintenance complaints. It connects three primary actors — **Residents**, **Property Managers**, and **Maintenance Staff** — across a structured property hierarchy (Property → Building → Unit). The system features role-based access control, smart algorithmic dispatch, media evidence handling, emergency SOS protocols, property-wide outage broadcasts, and a real-time manager dashboard.

All source code, documentation, infrastructure definitions, packages, and tooling live in a single monorepo.

### Why a monorepo?

| Concern | Benefit |
|---|---|
| **Atomic changes** | A single commit updates mobile, backend, packages, and docs simultaneously, keeping history coherent. |
| **Shared packages** | Shared API contracts, design tokens, and types live in `packages/` — one source of truth consumed by all clients. |
| **Shared conventions** | Naming rules, linting, and CI/CD pipelines are defined once and apply everywhere. |
| **Discoverability** | Any contributor can navigate the full system without switching repositories. |
| **AI collaboration** | AI-generated artifacts have an unambiguous home governed by rules defined in `docs/00_Project_Constitution/ai_collaboration_rules.md`. |
| **Dependency traceability** | Cross-cutting changes (e.g. a new domain concept) are visible across every layer in a single pull request. |

The monorepo is **not** a monolith. Each application and service remains individually deployable. The repository organises the code; runtime boundaries are enforced by deployment configuration.

**Nx** is the designated workspace orchestration tool. It manages the polyglot dependency graph across .NET, TypeScript, Kotlin, and Swift build systems, provides affected-project detection for CI, and enables distributed task caching. A root-level `nx.json` configuration file governs the task pipeline.

---

## 2. Top-Level Repository Structure

```
ACLSapp/
│
├── docs/                        # All engineering documentation
├── apps/                        # User-facing application projects
│   ├── ResidentApp.Android/     # Kotlin + Jetpack Compose — Residents only
│   ├── ResidentApp.iOS/         # Swift + SwiftUI — Residents only
│   ├── StaffApp.Android/        # Kotlin + Jetpack Compose — Maintenance Staff only
│   ├── StaffApp.iOS/            # Swift + SwiftUI — Maintenance Staff only
│   └── ResidentApp.Web/         # React + Next.js + TypeScript — Property Manager Dashboard
│
├── backend/                     # Server-side system (Clean Architecture / DDD)
│   ├── ACLS.sln                 ← solution entry point
│   ├── ACLS.SharedKernel/
│   ├── ACLS.Api/
│   ├── ACLS.Application/
│   ├── ACLS.Domain/
│   ├── ACLS.Infrastructure/
│   ├── ACLS.Persistence/
│   ├── ACLS.Contracts/
│   └── ACLS.Worker/
│
├── packages/                    # Shared cross-client packages
│   ├── api-contracts/
│   ├── design-tokens/
│   ├── shared-types/
│   ├── sdk/
│   ├── error-codes/
│   └── typescript-config/
│
├── tests/                       # All automated tests
│   ├── unit/
│   ├── integration/
│   ├── contract/
│   ├── e2e/
│   └── performance/
│
├── infrastructure/              # Cloud, CI/CD, and environment definitions
│   ├── terraform/
│   │   ├── modules/
│   │   └── environments/
│   │       ├── dev/
│   │       ├── staging/
│   │       └── prod/
│   └── pipelines/
│
├── tools/                       # Developer scripts and automation
│   ├── dev-environment/
│   │   ├── docker-compose.yml
│   │   └── bootstrap.sh
│   ├── scripts/
│   └── code-generation/
│
├── .github/
│   ├── CODEOWNERS
│   └── workflows/
│       ├── backend-ci.yml
│       ├── mobile-ci.yml
│       ├── web-ci.yml
│       ├── shared-packages-ci.yml
│       └── infrastructure-ci.yml
│
├── nx.json
├── .editorconfig
├── .gitignore
└── README.md
```

### Directory Responsibilities

| Directory | Responsibility |
|---|---|
| **`docs/`** | Single source of truth for all project knowledge. No documentation lives outside this directory. |
| **`apps/`** | One sub-project per user-facing application. Application code, assets, and platform-specific configuration. |
| **`backend/`** | The ASP.NET Core backend. `ACLS.sln` is the single solution file that references all layer projects. |
| **`packages/`** | TypeScript shared packages consumed by the web client. Eliminates type drift. |
| **`tests/`** | Centralised test suite covering all layers. Separated by type to allow CI to run each category independently. |
| **`infrastructure/`** | Terraform definitions for Azure resources and CI/CD pipeline YAML. No application business logic. |
| **`tools/`** | Local dev environment bootstrap, automation scripts, and code-generation utilities. Not shipped. |
| **`.github/workflows/`** | GitHub Actions CI/CD pipeline definitions, one per application surface. |

---

## 3. Documentation Architecture

All engineering documentation is stored under `docs/`. Sections are numbered to impose a logical reading order and make navigation stable across editors and AI tooling.

```
docs/
├── 00_Project_Constitution/
│   ├── project_charter.md
│   ├── repository_blueprint.md          ← this document
│   ├── ai_collaboration_rules.md        ← mandatory; governs all AI output
│   ├── glossary.md
│   ├── contributing.md
│   └── architectural_decisions/
│       ├── 0001_clean_architecture.md
│       ├── 0002_mssql_efcore.md
│       ├── 0003_media_storage_abstraction.md
│       └── 0004_notification_abstraction.md
│
├── 01_Product/
│   ├── product_requirements.md          ← functional + non-functional requirements (FR-01…FR-41, NFR-01…NFR-14)
│   ├── user_stories/
│   └── roadmap.md
│
├── 02_Domain/
│   ├── domain_overview.md
│   ├── ubiquitous_language.md           ← canonical term definitions (see Section 3a)
│   └── bounded_contexts/
│       ├── identity.md
│       ├── complaints.md
│       ├── dispatch.md
│       ├── notifications.md
│       ├── outages.md
│       └── reporting.md
│
├── 03_Architecture/
│   ├── system_overview.md
│   ├── container_diagram.md
│   ├── component_diagrams/
│   ├── technology_radar.md
│   └── security_model.md
│
├── 04_Data/
│   ├── data_model_overview.md
│   ├── schema/
│   │   └── erd.md
│   └── data_dictionary.md
│
├── 05_API/
│   ├── api_overview.md                  ← versioning strategy, auth scheme
│   ├── openapi/
│   │   └── v1.yaml
│   └── error_codes.md
│
├── 06_UML/
│   ├── class_diagram.puml               ← mirrors the provided UML diagram
│   ├── sequence_submit_complaint.puml
│   ├── sequence_assign_complaint.puml
│   ├── sequence_resolve_complaint.puml
│   ├── sequence_sos_trigger.puml
│   ├── sequence_declare_outage.puml
│   └── state_machine_ticket_status.puml
│
├── 07_Implementation/
│   ├── coding_standards.md
│   ├── clean_architecture_guide.md
│   ├── testing_strategy.md
│   ├── observability.md
│   └── patterns/
│       ├── dispatch_algorithm.md
│       ├── media_upload_pattern.md
│       └── multi_tenancy_pattern.md
│
├── 08_UX/
│   ├── design_principles.md
│   ├── design_system.md
│   ├── wireframes/
│   └── user_flows/
│       ├── resident_complaint_flow.md
│       ├── manager_assign_flow.md
│       └── staff_resolve_flow.md
│
├── 09_Operations/
│   ├── runbooks/
│   ├── incident_response.md
│   └── slo_definitions.md
│
└── 10_Security/
    ├── threat_model.md
    ├── owasp_checklist.md
    └── data_classification_policy.md
```

### 3a. Critical Glossary Terms (`docs/02_Domain/ubiquitous_language.md`)

> These terms must be used consistently and never interchanged in code or documentation.

| Term | Definition |
|---|---|
| **Property** | A physical apartment complex managed on the platform. The root of the tenancy hierarchy. |
| **Unit** | An individual apartment within a Building within a Property. A Resident is linked to a Unit. |
| **Resident** | A person who occupies a Unit and has authenticated access to submit and track complaints. |
| **Manager / Property Manager** | An administrator role with full read/write/assign access across a Property. |
| **Staff / Maintenance Staff** | A worker role that receives task assignments and resolves complaints. |
| **Complaint / Ticket** | The central domain entity. A maintenance request raised by a Resident. |
| **SOS** | An emergency complaint with `Urgency = SOS_EMERGENCY` that bypasses the standard queue and simultaneously notifies all on-call staff. |
| **Outage** | A property-wide event declared by a Manager (e.g. planned power outage) that triggers a mass broadcast. |
| **TAT (Turn-Around Time)** | The elapsed duration between `createdAt` and `resolvedAt` on a Complaint. Calculated asynchronously. |
| **Smart Dispatch** | The algorithmic service (`IDispatchService`) that scores and ranks available Staff for a given Complaint based on skill match and idle time. |
| **PropertyId** | Strongly-typed identifier. The mandatory multi-tenancy discriminator on every property-scoped entity. |

---

## 4. Application Projects

```
apps/
├── ResidentApp.Android/    # Native Android (Kotlin + Jetpack Compose) — Residents only
├── ResidentApp.iOS/        # Native iOS (Swift + SwiftUI) — Residents only
├── StaffApp.Android/       # Native Android (Kotlin + Jetpack Compose) — Maintenance Staff only
├── StaffApp.iOS/           # Native iOS (Swift + SwiftUI) — Maintenance Staff only
└── ResidentApp.Web/        # React + Next.js + TypeScript — Property Manager Dashboard (desktop-first)
```

All applications communicate with the backend exclusively via the versioned REST API. **No business logic, validation logic, or sorting/ranking algorithms live on any client.** Clients are thin rendering layers only.

| App | Platform | Target Users | Primary Screens |
|---|---|---|---|
| `ResidentApp.Android` | Kotlin + Jetpack Compose (API 26+) | Residents | Register, Login, Submit Complaint, SOS, Track Status, View History, Feedback |
| `ResidentApp.iOS` | Swift + SwiftUI (iOS 16+) | Residents | Register, Login, Submit Complaint, SOS, Track Status, View History, Feedback |
| `StaffApp.Android` | Kotlin + Jetpack Compose (API 26+) | Maintenance Staff | Login, My Tasks, Update Availability, Accept Job, Update ETA, Upload Completion Photos, Mark Resolved |
| `StaffApp.iOS` | Swift + SwiftUI (iOS 16+) | Maintenance Staff | Login, My Tasks, Update Availability, Accept Job, Update ETA, Upload Completion Photos, Mark Resolved |
| `ResidentApp.Web` | React + Next.js + TypeScript | Property Managers | Triage Queue, Assign/Reassign, Staff Dashboard, Onboarding Hub, Declare Outage, History & Analytics |

### Feature Scope per Client

#### ResidentApp (Android + iOS)
- Account registration via secure invitation token (linked to Unit)
- Secure login / logout
- Submit complaint: Category, Subject, Description, Urgency, Permission-to-Enter toggle
- Upload up to 3 evidence photos (device camera / gallery)
- SOS Panic Button — distinct high-visibility trigger; marks ticket as `SOS_EMERGENCY`
- Live status tracker — timeline view (Submitted → Assigned → En Route → In Progress → Resolved)
- ETA display — updated in real time when Staff changes estimated completion
- Complaint history — TAT, resolving staff member's name, resolution details
- Feedback — 1–5 star rating + optional comment after resolution
- Push notifications + in-app alerts for status changes, ETA updates, outage broadcasts

#### StaffApp (Android + iOS)
- Secure login / logout
- "My Active Jobs" list — sorted by urgency (server-side)
- Accept assignment — marks ticket as `EN_ROUTE`
- Status toggles: En Route → Working → Done
- ETA field — staff sets/updates estimated completion time
- Work notes — freetext notes on active ticket
- Upload completion photos
- Mark Resolved — triggers resident notification (backend-driven)
- Availability toggle — Available / Busy / On Break

#### ResidentApp.Web (Manager Dashboard)
- Secure login / logout
- Triage queue — all open complaints, filterable by Urgency, Category, Status, Date
- Assign complaint — visual staff availability indicators + skill tags; single-click assign
- Reassign complaint
- Real-time dashboard — open + in-progress complaints, current task assignments
- Staff performance history — tasks per staff member, average resident rating, TAT
- Unit history — all complaints per apartment unit, recurring issue detection
- Declare Outage — form (type, time window, description); triggers mass SMS/email blast
- Onboarding Hub — CSV batch-upload for units; generate + send invitation emails to residents
- Search complaints by category, status, date range
- Filter complaints by priority
- Generate summary reports
- Manage user accounts (activate/deactivate)

---

## 5. Backend System Structure

The backend lives under `backend/`. A single solution file (`ACLS.sln`) references all layer projects.

### Solution File

`backend/ACLS.sln` is the only `.sln` file in the repository. It registers every `ACLS.*` project so that `dotnet build backend/ACLS.sln` compiles the entire backend in one command.

### Project Layout

```
backend/
├── ACLS.sln
├── ACLS.SharedKernel/        # Strongly-typed IDs, base entity classes, domain interfaces,
│                             # Result<T> types, Guard clauses — zero external dependencies
├── ACLS.Api/                 # ASP.NET Core: controllers, middleware, auth guards,
│                             # tenancy middleware, OpenAPI (Swashbuckle)
├── ACLS.Application/         # Use cases, commands/queries (MediatR), DTOs, FluentValidation
├── ACLS.Domain/              # Entities, aggregates, value objects, domain events,
│                             # domain service interfaces (IDispatchService, INotificationService,
│                             # IStorageService, IReportingService)
├── ACLS.Infrastructure/      # Adapters: IStorageService (Azure Blob / S3),
│                             # INotificationService (abstract — provider pluggable),
│                             # ISmtpClient, ISmsClient
├── ACLS.Persistence/         # EF Core DbContext, repository implementations, migrations
│   └── Migrations/
├── ACLS.Contracts/           # Shared request/response DTOs exposed between API and clients
│                             # Source of truth for API shape — TypeScript sdk package mirrors this
└── ACLS.Worker/              # Background jobs: async notification dispatch, TAT calculation,
                              # average rating recalculation, audit log writes
```

### Dependency Rule (Strict — Never Violated)

```
# Arrow direction: A → B means "A depends on B"

SharedKernel ← Domain ← Application ← Infrastructure
                                     ← Persistence
              Api → Application
              Api → Contracts
              Contracts → (zero inward dependencies — consumed by Api and external clients)
              Worker → Application
```

| Project | Layer | Allowed Dependencies |
|---|---|---|
| `ACLS.SharedKernel` | Shared kernel | None — zero external references |
| `ACLS.Domain` | Core domain | `ACLS.SharedKernel` |
| `ACLS.Application` | Application | `ACLS.Domain`, `ACLS.SharedKernel` |
| `ACLS.Infrastructure` | Infrastructure | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` |
| `ACLS.Persistence` | Infrastructure (data) | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` |
| `ACLS.Api` | Presentation | `ACLS.Application`, `ACLS.Contracts` |
| `ACLS.Contracts` | API contracts | None |
| `ACLS.Worker` | Background processing | `ACLS.Application`, `ACLS.SharedKernel` |

> **Critical rule:** `ACLS.Domain` must have zero external package references. All domain service dependencies are expressed as interfaces in the Domain layer and implemented in Infrastructure.

### Feature Folder Structure

`ACLS.Domain` and `ACLS.Application` are organised by **bounded context (feature)**, not by technical type. This keeps all code for a domain concept co-located and enables future extraction.

```
ACLS.Domain/
├── Identity/
│   ├── User.cs                       # Base user entity (userId, email, passwordHash, phone, isActive, propertyId, role)
│   ├── Role.cs                       # Enum — Resident, Manager, MaintenanceStaff
│   ├── InvitationToken.cs            # Value object — secure token linking registration to a Unit
│   ├── IUserRepository.cs
│   └── Events/
│       └── UserRegisteredEvent.cs
│
├── Properties/
│   ├── Property.cs                   # Aggregate root — propertyId, name, address
│   ├── Building.cs                   # Entity — buildingId, name, FK→Property
│   ├── Unit.cs                       # Entity — unitId, unitNumber, floor, FK→Building
│   ├── PropertyId.cs                 # Strongly-typed value object — multi-tenancy discriminator
│   ├── IPropertyRepository.cs
│   └── Events/
│
├── Residents/
│   ├── Resident.cs                   # Extends User — leaseStart, leaseEnd, FK→Unit
│   ├── IResidentRepository.cs
│   └── Events/
│
├── Staff/
│   ├── StaffMember.cs                # Extends User — jobTitle, skills (List<string>),
│   │                                 # availability (StaffState), averageRating, lastAssignedAt
│   ├── StaffState.cs                 # Enum — AVAILABLE, BUSY, ON_BREAK, OFF_DUTY
│   ├── IStaffRepository.cs
│   └── Events/
│       └── StaffAvailabilityChangedEvent.cs
│
├── Complaints/
│   ├── Complaint.cs                  # Aggregate root — ticketId, title, description, urgency,
│   │                                 # status, permissionToEnter, createdAt, eta, resolvedAt,
│   │                                 # residentRating, residentFeedback, FK→Unit, FK→Staff
│   ├── Urgency.cs                    # Enum — LOW, MEDIUM, HIGH, SOS_EMERGENCY
│   ├── TicketStatus.cs               # Enum — OPEN, ASSIGNED, EN_ROUTE, IN_PROGRESS, RESOLVED, CLOSED
│   ├── Media.cs                      # Entity — mediaId, url (blob URL), type, FK→Complaint
│   ├── WorkNote.cs                   # Entity — noteId, content, createdAt, FK→Complaint, FK→Staff
│   ├── IComplaintRepository.cs
│   └── Events/
│       ├── ComplaintSubmittedEvent.cs
│       ├── ComplaintAssignedEvent.cs
│       ├── ComplaintStatusChangedEvent.cs
│       └── ComplaintResolvedEvent.cs
│
├── Dispatch/
│   ├── IDispatchService.cs           # Interface — FindOptimalStaff(complaint) → List<StaffScore>
│   ├── StaffScore.cs                 # Value object — Staff + MatchScore (skill match + idle time)
│   └── DispatchCriteria.cs           # Value object — required skills, urgency weight
│
├── Notifications/
│   ├── INotificationService.cs       # Interface — NotifyResident, NotifyStaff, BroadcastOutage,
│   │                                 #             NotifyAllOnCallStaff (SOS)
│   ├── NotificationChannel.cs        # Enum — Email, SMS, InApp, Push
│   └── NotificationTemplate.cs       # Value object — template key + channel
│
├── Outages/
│   ├── Outage.cs                     # Aggregate root — title, outageType, startTime, endTime,
│   │                                 # description, FK→Property
│   ├── OutageType.cs                 # Enum — Electricity, Water, Gas, Internet, Other
│   ├── IOutageRepository.cs
│   └── Events/
│       └── OutageDeclaredEvent.cs
│
├── Storage/
│   └── IStorageService.cs            # Interface — UploadAsync(stream, fileName) → string (URL)
│                                     #           — GeneratePresignedUrl(key) → string
│
├── AuditLog/
│   ├── AuditEntry.cs                 # Immutable record — action, entityId, actorId, timestamp,
│   │                                 # oldValue, newValue
│   ├── IAuditRepository.cs
│   └── AuditAction.cs                # Enum — ComplaintCreated, StatusChanged, Assigned, etc.
│
└── Reporting/
    ├── IReportingService.cs          # Interface — CalculateStaffTAT, GetComplaintsByUnit,
    │                                 #             GetStaffPerformanceSummary
    └── StaffPerformanceSummary.cs    # Read model — staff name, totalResolved, averageRating, avgTAT
```

```
ACLS.Application/
├── Identity/
│   ├── Commands/
│   │   ├── RegisterResident/
│   │   │   ├── RegisterResidentCommand.cs
│   │   │   └── RegisterResidentCommandHandler.cs
│   │   └── LoginUser/
│   │       ├── LoginUserCommand.cs
│   │       └── LoginUserCommandHandler.cs
│   ├── Queries/
│   │   └── GetCurrentUser/
│   └── DTOs/
│       ├── UserDto.cs
│       └── AuthTokenDto.cs
│
├── Complaints/
│   ├── Commands/
│   │   ├── SubmitComplaint/
│   │   │   ├── SubmitComplaintCommand.cs      ← accepts multipart; triggers IStorageService first
│   │   │   └── SubmitComplaintCommandHandler.cs
│   │   ├── AssignComplaint/
│   │   │   ├── AssignComplaintCommand.cs      ← single transaction: ASSIGNED + Staff→BUSY
│   │   │   └── AssignComplaintCommandHandler.cs
│   │   ├── UpdateComplaintStatus/
│   │   ├── ResolveComplaint/
│   │   │   ├── ResolveComplaintCommand.cs     ← RESOLVED + Staff→AVAILABLE + notify resident
│   │   │   └── ResolveComplaintCommandHandler.cs
│   │   ├── AddWorkNote/
│   │   ├── UpdateEta/
│   │   ├── TriggerSos/                        ← marks SOS_EMERGENCY + notifies all on-call staff
│   │   ├── SubmitFeedback/
│   │   └── ReassignComplaint/
│   ├── Queries/
│   │   ├── GetComplaintById/
│   │   ├── GetComplaintsByResident/
│   │   ├── GetAllComplaints/                  ← manager-only; supports filter/search
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
│   │       └── GetDispatchRecommendationsQueryHandler.cs    ← calls IDispatchService
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
├── Outages/
│   ├── Commands/
│   │   └── DeclareOutage/
│   │       ├── DeclareOutageCommand.cs        ← creates Outage + triggers BroadcastOutage
│   │       └── DeclareOutageCommandHandler.cs
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
└── UserManagement/
    ├── Commands/
    │   ├── InviteResident/
    │   ├── DeactivateUser/
    │   └── ReactivateUser/
    └── DTOs/
        └── InvitationDto.cs
```

### Critical Business Logic — Implementation Rules

These workflows are non-negotiable. All implementations must precisely follow these rules.

#### 1. Submit Complaint (Media Handling)
```
Controller → SubmitComplaintCommandHandler:
  1. Receive multipart form data (complaint fields + up to 3 image files)
  2. For EACH file:
       url = await IStorageService.UploadAsync(fileStream, fileName)
       media = new Media(url, fileType, complaintId)
  3. Persist Complaint entity to DB
  4. Persist Media entities to DB (linked by FK to Complaint)
  5. Raise ComplaintSubmittedEvent → NotificationService
```
> Binary files are NEVER stored in MSSQL. Only blob URLs are persisted in the Media table.

#### 2. Assign Complaint (Atomic Transaction)
```
Controller → AssignComplaintCommandHandler:
  1. Begin database transaction
  2. complaint.Status = ASSIGNED; complaint.AssignedStaffId = staffId
  3. staff.Availability = BUSY; staff.LastAssignedAt = DateTime.UtcNow
  4. Commit transaction (both changes atomic — neither persists without the other)
  5. Raise ComplaintAssignedEvent → NotificationService (notify staff of new assignment)
  6. Write AuditEntry
```

#### 3. Resolve Complaint
```
Controller → ResolveComplaintCommandHandler:
  1. complaint.Status = RESOLVED; complaint.ResolvedAt = DateTime.UtcNow
  2. staff.Availability = AVAILABLE
  3. Commit transaction
  4. Raise ComplaintResolvedEvent → NotificationService (notify resident)
  5. Queue async TAT calculation job → ACLS.Worker
  6. Queue async average rating recalculation if feedback exists → ACLS.Worker
  7. Write AuditEntry
```

#### 4. Smart Dispatch Algorithm (`IDispatchService`)
```
DispatchService.FindOptimalStaff(complaint):
  1. Query all Staff WHERE availability = AVAILABLE AND propertyId = complaint.propertyId
  2. For each candidate staff:
       skillScore    = |intersection(staff.skills, complaint.requiredSkills)| / |complaint.requiredSkills|
       idleScore     = NormalisedIdleTime(DateTime.UtcNow - staff.lastAssignedAt)
       urgencyWeight = complaint.urgency == SOS_EMERGENCY ? 2.0 : 1.0
       matchScore    = (skillScore * 0.6 + idleScore * 0.4) * urgencyWeight
  3. Return List<StaffScore> ordered by matchScore DESC
```

#### 5. SOS Trigger
```
TriggerSosCommandHandler:
  1. complaint.Urgency = SOS_EMERGENCY; complaint.Status = ASSIGNED
  2. Query all Staff WHERE availability = AVAILABLE AND propertyId = complaint.propertyId
  3. INotificationService.NotifyAllOnCallStaff(staffList, complaint)  ← simultaneous blast
  4. Write AuditEntry tagged as SOS
```

#### 6. Declare Outage
```
DeclareOutageCommandHandler:
  1. Persist Outage entity (type, startTime, endTime, description, propertyId)
  2. Raise OutageDeclaredEvent
  3. INotificationService.BroadcastOutage(outage)  ← mass SMS + email to all property residents
  4. Worker handles async fan-out (NFR-12: 500 messages within 60 seconds)
```

### API Versioning Strategy

**Approach:** URI path versioning (`/api/v1/...`)

All routes are prefixed `/api/v{major}/`. Only major (breaking) changes increment the version. Both old and new versions are supported in parallel for a minimum of 6 months after a new version ships. OpenAPI spec: `docs/05_API/openapi/v1.yaml`.

### Database Configuration

**Development:** Local SQL Server via `appsettings.Development.json`  
**Production:** Azure SQL Server — connection string injected via environment variable `ACLS_DB_CONNECTION`

```json
// appsettings.json (committed — no secrets)
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}

// appsettings.Development.json (gitignored)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ACLS_Dev;Trusted_Connection=True;"
  }
}
```

EF Core migrations:
```
dotnet ef migrations add <n> --project ACLS.Persistence --startup-project ACLS.Api
dotnet ef database update --project ACLS.Persistence --startup-project ACLS.Api
```

### Multi-Tenancy Enforcement

Every property-scoped entity carries `PropertyId`. The `TenancyMiddleware` in `ACLS.Api` reads the authenticated user's `PropertyId` claim and injects it as a scoped dependency. All repository queries automatically filter by `PropertyId`. Controllers never accept `PropertyId` from request bodies — it is always read from the authenticated identity context.

### Identity and Authentication

**Approach:** JWT Bearer tokens issued by `ACLS.Api` (self-issued) using ASP.NET Core Identity or a configurable OIDC provider.  
**Roles:** `Resident`, `Manager`, `MaintenanceStaff` — enforced via `[Authorize(Roles = "...")]` on controllers.  
**Invitation flow:** Manager generates invitation token → sent to resident via email → resident registers using token → account linked to Unit.  
**Token claims:** `userId`, `propertyId`, `role` — `propertyId` is the tenancy discriminator and is never sourced from request data.

### Observability

| Signal | Tooling |
|---|---|
| Structured logs | `Microsoft.Extensions.Logging` + OTel log bridge |
| Distributed traces | OpenTelemetry + ASP.NET Core instrumentation |
| Health checks | `/healthz` endpoint via `Microsoft.Extensions.Diagnostics.HealthChecks` |

---

## 6. Tests Directory

```
tests/
├── unit/
│   ├── Domain.Tests/              # Pure logic: DispatchService scoring, entity state machines
│   ├── Application.Tests/         # Command/query handler tests (mocked infrastructure)
│   └── Worker.Tests/              # Background job handler logic
│
├── integration/
│   ├── Persistence.Tests/         # EF Core against a real local SQL Server (TestContainers)
│   └── Infrastructure.Tests/      # Storage adapter, notification adapter tests
│
├── contract/
│   └── Api.ContractTests/         # Verify API shape matches Contracts project (Pact .NET)
│
├── e2e/
│   ├── Web.E2ETests/              # Playwright — Manager Dashboard flows
│   └── Api.E2ETests/              # HTTP-level flow tests (submit → assign → resolve)
│
└── performance/
    └── k6/                        # NFR-12: broadcast notification load test (500 msg / 60s)
```

| Suite | When it runs | Tooling |
|---|---|---|
| `unit/` | Every commit, every PR | NUnit, FluentAssertions, NSubstitute |
| `integration/` | Every PR | NUnit, TestContainers, Respawn |
| `contract/` | Every PR | Pact .NET |
| `e2e/` | Merge to `main`, Release | Playwright (Web), HttpClient (API) |
| `performance/` | On-demand / Release candidate | k6 |

> **NUnit is the mandatory test framework** as specified in project requirements. All unit and integration test projects use NUnit exclusively.

### Key Test Scenarios (Must Be Covered)

| Scenario | Test Type | Suite |
|---|---|---|
| DispatchService ranking — skill match weight | Unit | `Domain.Tests` |
| DispatchService ranking — idle time weight | Unit | `Domain.Tests` |
| DispatchService — SOS urgency multiplier | Unit | `Domain.Tests` |
| AssignComplaint — atomicity (staff BUSY on assign) | Integration | `Persistence.Tests` |
| ResolveComplaint — staff returns to AVAILABLE | Integration | `Persistence.Tests` |
| SubmitComplaint — media URL saved, no binary in DB | Integration | `Persistence.Tests` |
| Multi-tenancy — cross-property data isolation | Integration | `Persistence.Tests` |
| SOS — all on-call staff notified simultaneously | Unit | `Application.Tests` |
| DeclareOutage — broadcast triggers for all residents | Unit | `Application.Tests` |
| Full complaint lifecycle (submit → assign → resolve) | E2E | `Api.E2ETests` |

---

## 7. Packages Directory

```
packages/
├── api-contracts/         # Shared API request/response types (TypeScript)
│                          # Mirrors ACLS.Contracts — kept in sync manually or via generation
├── design-tokens/         # Colour, spacing, typography tokens for ResidentApp.Web
├── shared-types/          # Common enums: TicketStatus, Urgency, StaffState, Role
├── sdk/                   # Auto-generated typed API client (TypeScript/Axios)
│                          # Generated from docs/05_API/openapi/v1.yaml
├── error-codes/           # Typed error code constants — prevents hardcoded string comparisons
└── typescript-config/     # Centralised ESLint, tsconfig, Prettier rules for all Node projects
```

| Package | Consumers | Format |
|---|---|---|
| `api-contracts` | `ResidentApp.Web` | TypeScript / npm |
| `design-tokens` | `ResidentApp.Web` | Style Dictionary JSON → CSS variables |
| `shared-types` | `ResidentApp.Web` | TypeScript / npm |
| `sdk` | `ResidentApp.Web` | TypeScript / npm — auto-generated from OpenAPI |
| `error-codes` | `ResidentApp.Web` | TypeScript / npm |
| `typescript-config` | `ResidentApp.Web`, all `packages/` | JSON — shared config |

> **Note:** Android and iOS clients do not consume npm packages. They consume the REST API directly. Kotlin and Swift data classes/structs are generated or manually maintained within their respective app projects (`ResidentApp.Android/data/`, `StaffApp.Android/data/`, etc.), mirroring `ACLS.Contracts`.

> **Critical rule:** All API responses must use flat, strictly typed, deterministic JSON shapes. Polymorphic response types (`oneOf`, `anyOf`) must not be used — OpenAPI generators produce unusable Kotlin and Swift code from polymorphic schemas.

---

## 8. Infrastructure Directory

```
infrastructure/
├── terraform/
│   ├── modules/
│   │   ├── app-service/          # Azure App Service module (API hosting)
│   │   ├── sql-server/           # Azure SQL module
│   │   ├── blob-storage/         # Azure Blob Storage (media)
│   │   └── key-vault/            # Azure Key Vault (secrets)
│   └── environments/
│       ├── dev/
│       ├── staging/
│       └── prod/
│
└── pipelines/
```

All Azure resources are defined as Terraform HCL. Secrets are never committed — injected via Azure Key Vault references or GitHub Actions secrets.

### CI/CD Pipelines

```
.github/workflows/
├── backend-ci.yml           # Restore → Build → Unit tests → Integration tests → Contract tests
├── mobile-ci.yml            # Android Gradle build + NUnit; iOS xcodebuild + XCTest
├── web-ci.yml               # Install → Lint → Build → Unit tests → Playwright E2E (on main)
├── shared-packages-ci.yml   # Build all packages; validate downstream consumer compatibility
└── infrastructure-ci.yml    # terraform fmt → validate → plan (PR) → apply (main)
```

All pipelines use **Nx affected** to detect impacted projects. Full-repo builds are never triggered naively.

| Pipeline | Trigger path filter | Key steps |
|---|---|---|
| `backend-ci.yml` | `backend/**`, `tests/unit/**`, `tests/integration/**` | Build → Unit → Integration → Contract |
| `mobile-ci.yml` | `apps/ResidentApp.Android/**`, `apps/ResidentApp.iOS/**`, `apps/StaffApp.Android/**`, `apps/StaffApp.iOS/**` | Build + test per platform |
| `web-ci.yml` | `apps/ResidentApp.Web/**`, `packages/**` | Lint → Build → Unit → E2E (on `main`) |
| `shared-packages-ci.yml` | `packages/**` | Build all → validate consumers |
| `infrastructure-ci.yml` | `infrastructure/**` | Plan (PR), Apply (main) |

---

## 9. Tools Directory

```
tools/
├── dev-environment/
│   ├── docker-compose.yml        # Local SQL Server, Azurite (Blob emulator), MailHog (email)
│   └── bootstrap.sh              # One-command setup: install deps, run migrations, seed DB
│
├── scripts/
│   ├── seed-db.ps1               # Seed local DB with reference data (properties, units, users)
│   ├── clean.ps1                 # Repository clean-up
│   └── generate-invitation.ps1   # Generate test invitation tokens for local dev
│
└── code-generation/
    ├── generate-api-client.ps1   # Generate TypeScript sdk from OpenAPI v1.yaml
    └── generate-design-tokens.ps1
```

### Local Developer Environment

`docker-compose.yml` provides all external service dependencies without cloud access:

| Service | Image | Purpose |
|---|---|---|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` | Primary database |
| Azurite | `mcr.microsoft.com/azure-storage/azurite` | Local blob storage emulator |
| MailHog | `mailhog/mailhog` | Local email capture (notification testing) |

A developer onboarding for the first time runs:
```bash
./tools/dev-environment/bootstrap.sh
```

---

## 10. Naming Conventions

### Projects and Namespaces

| Context | Convention | Example |
|---|---|---|
| Backend projects | `ACLS.<Layer>` | `ACLS.Domain`, `ACLS.Api` |
| Resident app projects | `ResidentApp.<Platform>` | `ResidentApp.Android`, `ResidentApp.Web` |
| Staff app projects | `StaffApp.<Platform>` | `StaffApp.Android`, `StaffApp.iOS` |
| Test projects | `<Project>.Tests` | `Domain.Tests`, `Api.ContractTests` |
| C# namespaces | Match project name | `namespace ACLS.Domain.Complaints` |
| Interfaces (C#) | Prefix with `I` | `IComplaintRepository`, `IDispatchService` |
| npm packages | `@acls/<n>` | `@acls/api-contracts` |

### Directories

| Rule | Example |
|---|---|
| Top-level dirs: lowercase | `docs/`, `apps/`, `packages/`, `tests/` |
| App and backend project dirs: match project name | `ResidentApp.Web/`, `ACLS.Api/` |
| Documentation sections: numbered prefix + PascalCase | `03_Architecture/` |
| Feature folders inside projects: PascalCase | `Complaints/`, `Dispatch/` |

### Platform Code Style

| Platform | Standard |
|---|---|
| C# (Backend) | Microsoft C# Coding Conventions + `docs/07_Implementation/coding_standards.md` |
| Kotlin (Android) | Kotlin coding conventions + Android Kotlin style guide |
| Swift (iOS) | Swift API Design Guidelines |
| TypeScript (Web) | Project ESLint + Prettier ruleset (React + Next.js conventions) |

---

## 11. AI Collaboration Rules

> These rules are the canonical reference. The authoritative file is `docs/00_Project_Constitution/ai_collaboration_rules.md`. Violations must be caught at pull request review. No AI-generated artifact that breaks these rules should be merged.

### Documentation Rules
1. All documentation must be written under `docs/`. Never place docs in `apps/`, `backend/`, or `infrastructure/`.
2. Documentation must land in the correct numbered section. An API spec belongs in `docs/05_API/`, not `docs/03_Architecture/`.
3. ADRs must be created in `docs/00_Project_Constitution/architectural_decisions/` using the Nygard format.

### Code Rules
4. **Backend code** goes in `backend/ACLS.<Layer>/`. Match the layer to the responsibility.
5. **Resident client code** goes in `apps/ResidentApp.<Platform>/`.
6. **Staff client code** goes in `apps/StaffApp.<Platform>/`.
7. **Shared cross-client types** go in `packages/`. Do not duplicate them in app projects.
8. The **Clean Architecture dependency rule** must never be violated. `ACLS.Domain` must have zero external project or package references.
9. New C# classes must follow the namespace convention: `ACLS.<Layer>.<Feature>`.
10. **Controllers must never write SQL or access the ORM directly.** Controllers delegate to Application layer command/query handlers via MediatR. Handlers use Infrastructure repositories.
11. **Binary files are never stored in MSSQL.** Always upload to blob storage first; persist only the URL.
12. **`PropertyId` is always read from the authenticated JWT claim** — never from request body parameters.

### Architecture Rules
13. Consult `docs/03_Architecture/` before proposing new service boundaries or technology choices.
14. New external dependencies must be justified in the PR description.
15. Cross-cutting concerns (logging, auth, multi-tenancy, caching) must use the approved patterns in `docs/07_Implementation/`.
16. Secrets must never be committed. Use Key Vault references or GitHub Actions secrets.

### Structure Rules
17. **New modules must align with the defined structure.** New bounded context → projects in `backend/` + docs in `docs/02_Domain/bounded_contexts/`.
18. New top-level directories require an approved ADR.
19. Infrastructure changes must use Terraform HCL under `infrastructure/terraform/`.
20. Tests must be placed in the appropriate `tests/<type>/` directory — not inside application projects.

---

## 12. Mobile App Internal Structure

Both Android platforms (Resident + Staff) and both iOS platforms (Resident + Staff) follow the same internal layering pattern.

### Android (Kotlin + Jetpack Compose)

```
ResidentApp.Android/   (and StaffApp.Android/ — same structure)
├── app/
│   └── src/main/
│       ├── java/com/acls/<appname>/
│       │   ├── data/
│       │   │   ├── remote/
│       │   │   │   ├── ApiService.kt        # Retrofit interface — all API calls
│       │   │   │   ├── ApiClient.kt         # Retrofit builder (base URL from BuildConfig)
│       │   │   │   └── dto/                 # Data classes mirroring ACLS.Contracts
│       │   │   ├── repository/              # Repository implementations
│       │   │   └── local/                   # Room DB (optional: offline draft complaints)
│       │   ├── domain/
│       │   │   ├── model/                   # Pure Kotlin models (no Android dependencies)
│       │   │   └── repository/              # Repository interfaces
│       │   ├── ui/
│       │   │   ├── screens/                 # One package per screen (Composable + ViewModel)
│       │   │   ├── components/              # Shared Composable components
│       │   │   ├── navigation/              # NavGraph definition
│       │   │   └── theme/                   # MaterialTheme tokens
│       │   └── di/
│       │       └── AppModule.kt             # Hilt dependency injection module
│       └── res/
├── build.gradle.kts
└── proguard-rules.pro
```

> All network calls go through `ApiService.kt` (Retrofit). No business logic in `ViewModel` — ViewModels call repository methods and expose `StateFlow` for UI to collect. All sorting, filtering, and ranking is done by the backend and consumed as-is.

### iOS (Swift + SwiftUI)

```
ResidentApp.iOS/   (and StaffApp.iOS/ — same structure)
├── Sources/
│   ├── App/
│   │   └── ACLSApp.swift                    # Entry point + dependency injection setup
│   ├── Data/
│   │   ├── Network/
│   │   │   ├── APIClient.swift              # URLSession-based API client (base URL from env)
│   │   │   └── DTOs/                        # Codable structs mirroring ACLS.Contracts
│   │   └── Repositories/                    # Protocol implementations
│   ├── Domain/
│   │   ├── Models/                          # Pure Swift models
│   │   └── Repositories/                    # Repository protocols
│   ├── UI/
│   │   ├── Screens/                         # One folder per screen (View + ViewModel)
│   │   ├── Components/                      # Shared SwiftUI views
│   │   └── Navigation/                      # NavigationPath / Router
│   └── Core/
│       ├── DI/                              # Dependency injection (custom or Swift DI pattern)
│       └── Extensions/
├── Resources/
│   ├── Assets.xcassets
│   └── Info.plist
└── Tests/
    └── (unit tests — XCTest)
```

---

## 13. Web App Internal Structure (ResidentApp.Web — Manager Dashboard)

```
ResidentApp.Web/
├── src/
│   ├── app/                              # Next.js App Router pages
│   │   ├── (auth)/
│   │   │   └── login/
│   │   ├── dashboard/
│   │   │   ├── page.tsx                  # Real-time complaint dashboard
│   │   │   └── layout.tsx
│   │   ├── complaints/
│   │   │   ├── page.tsx                  # Triage queue
│   │   │   └── [id]/
│   │   │       └── page.tsx              # Complaint detail / assign
│   │   ├── staff/
│   │   │   └── page.tsx                  # Staff availability + performance
│   │   ├── outages/
│   │   │   └── page.tsx                  # Declare outage form
│   │   ├── onboarding/
│   │   │   └── page.tsx                  # CSV upload + invite residents
│   │   ├── reports/
│   │   │   └── page.tsx                  # Historical reports
│   │   └── settings/
│   │       └── page.tsx                  # User account management
│   ├── components/
│   │   ├── ui/                           # Generic UI components (Button, Badge, Table, etc.)
│   │   ├── complaints/                   # Complaint-specific components
│   │   ├── dispatch/                     # Staff dispatch recommendation panel
│   │   └── layout/                       # Sidebar, Header, Navigation
│   ├── lib/
│   │   ├── api/                          # API client (from @acls/sdk or manually written)
│   │   └── auth/                         # Auth token management
│   └── types/                            # Local TypeScript types (supplement @acls/shared-types)
├── public/
├── next.config.ts
├── tailwind.config.ts
└── tsconfig.json
```

---

## 14. Future Evolution

### Potential Phase 2 Features
- **Resident-to-Manager messaging** — in-app messaging thread per complaint ticket
- **Push notifications** — FCM (Android) / APNs (iOS) for real-time status updates
- **Smart Dispatch v2** — historical resolution time factored into scoring
- **Resident Portal Web** — browser-based access for residents (new `ResidentApp.Web.Portal` app)
- **Analytics export** — CSV/PDF report generation for managers (FR-31)

### Extracting Backend Services
As the system grows, high-throughput services may be extracted:
```
backend/
├── ACLS.Api/             # Core complaint management API
├── ACLS.Notifications/   # Extracted notification service (own CA layers)
└── ACLS.Reporting/       # Extracted reporting service (read-side projections)
```

Shared types between extracted services are already available via `ACLS.SharedKernel`.

### Scaling the Broadcast Service (NFR-12)
The Worker project is designed to fan-out outage notifications asynchronously. The `ACLS.Worker` project uses a message queue pattern (`OutageDeclaredEvent → Worker`) to dispatch 500+ SMS/email messages within 60 seconds without blocking the main API thread or causing database locking.

---

*End of ACLS Repository Blueprint v1.0*
