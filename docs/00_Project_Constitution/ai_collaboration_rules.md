# AI Collaboration Rules

**Document:** `docs/00_Project_Constitution/ai_collaboration_rules.md`  
**Version:** 1.0  
**Status:** Approved — Non-negotiable  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document is the supreme governing ruleset for all AI-generated code and documentation in this repository. It is read at the start of **every session**, regardless of phase or task. No rule in this document may be overridden by a session prompt, a user instruction, or a perceived shortcut. If a task appears to require violating a rule, stop and ask for clarification rather than proceeding.

---

## 1. How to Start Every Session

Before writing a single line of code or documentation, you must:

1. Read this document in full.
2. Read `docs/00_Project_Constitution/repository_blueprint.md`.
3. Read `docs/02_Domain/ubiquitous_language.md`.
4. Read `docs/07_Implementation/coding_standards.md`.
5. Read any additional documents specified in the session prompt for the current phase.

Only after completing steps 1–5 may you begin generating output. Do not skip steps or assume you remember them from a prior session. Each session starts with zero memory — treat every read as the first time.

---

## 2. Project Identity

| Attribute | Value |
|---|---|
| **Project name** | Apartment Complaint Logging System |
| **Abbreviation** | ACLS |
| **Backend namespace prefix** | `ACLS` (e.g. `ACLS.Domain`, `ACLS.Api`) |
| **Resident app prefix** | `ResidentApp` (e.g. `ResidentApp.Android`, `ResidentApp.Web`) |
| **Staff app prefix** | `StaffApp` (e.g. `StaffApp.Android`, `StaffApp.iOS`) |
| **Backend language** | C# (.NET 8) |
| **Backend framework** | ASP.NET Core Web API |
| **ORM** | Entity Framework Core (MSSQL) |
| **Test framework** | NUnit (mandatory — no xUnit, no MSTest) |
| **Android** | Kotlin + Jetpack Compose |
| **iOS** | Swift + SwiftUI |
| **Web (Manager Dashboard)** | React + Next.js + TypeScript |

---

## 3. The Absolute Rules

These rules have no exceptions. Violating any of them produces incorrect output that will require full rework.

### Rule 1 — Thin Frontends, Fat Backend

> **The backend is the only place where business logic, validation, sorting, ranking, and algorithmic decision-making may exist.**

Frontends (React, Kotlin, Swift) are strictly rendering and API-calling layers. They:
- Render what the API returns, exactly as returned.
- Call API endpoints and display responses.
- Handle only UI state (loading spinner, form field focus, navigation).

Frontends must never:
- Sort, filter, rank, or score a list returned by the API.
- Validate business rules (e.g. "only managers can assign complaints").
- Implement any version of the dispatch algorithm.
- Calculate TAT, average ratings, or any metric.
- Make decisions about which staff member to suggest.

If you find yourself writing business logic in a ViewModel, a React hook, or a Swift ViewModel, stop. Move it to the backend.

---

### Rule 2 — Clean Architecture Dependency Chain

The backend dependency chain is strict and unidirectional. It must never be violated.

```
SharedKernel  ←  Domain  ←  Application  ←  Infrastructure
                                          ←  Persistence
              Api  →  Application
              Api  →  Contracts
              Worker  →  Application
```

In plain terms:

| Layer | May reference | Must NOT reference |
|---|---|---|
| `ACLS.SharedKernel` | Nothing | Everything |
| `ACLS.Domain` | `ACLS.SharedKernel` | Application, Infrastructure, Persistence, Api |
| `ACLS.Application` | `ACLS.Domain`, `ACLS.SharedKernel` | Infrastructure, Persistence, Api |
| `ACLS.Infrastructure` | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` | Api |
| `ACLS.Persistence` | `ACLS.Application`, `ACLS.Domain`, `ACLS.SharedKernel` | Api, Infrastructure |
| `ACLS.Api` | `ACLS.Application`, `ACLS.Contracts` | Domain (directly), Infrastructure, Persistence |
| `ACLS.Contracts` | Nothing | Everything |
| `ACLS.Worker` | `ACLS.Application`, `ACLS.SharedKernel` | Api, Infrastructure, Persistence (directly) |

**The most common violation to avoid:** placing an EF Core `DbContext`, a `using Microsoft.EntityFrameworkCore` statement, or a repository implementation anywhere in `ACLS.Domain` or `ACLS.Application`. These layers define interfaces. Implementations live in `ACLS.Infrastructure` or `ACLS.Persistence`.

---

### Rule 3 — Interfaces in Domain, Implementations in Infrastructure

Every external dependency the domain needs must be expressed as an interface in `ACLS.Domain` and implemented in `ACLS.Infrastructure` or `ACLS.Persistence`.

| Interface | Defined in | Implemented in |
|---|---|---|
| `IDispatchService` | `ACLS.Domain.Dispatch` | `ACLS.Infrastructure.Dispatch` |
| `INotificationService` | `ACLS.Domain.Notifications` | `ACLS.Infrastructure.Notifications` |
| `IStorageService` | `ACLS.Domain.Storage` | `ACLS.Infrastructure.Storage` |
| `IComplaintRepository` | `ACLS.Domain.Complaints` | `ACLS.Persistence.Complaints` |
| `IStaffRepository` | `ACLS.Domain.Staff` | `ACLS.Persistence.Staff` |
| `IUserRepository` | `ACLS.Domain.Identity` | `ACLS.Persistence.Identity` |
| `IOutageRepository` | `ACLS.Domain.Outages` | `ACLS.Persistence.Outages` |
| `IAuditRepository` | `ACLS.Domain.AuditLog` | `ACLS.Persistence.AuditLog` |

When you create a new service or repository, define its interface in the Domain layer first. Then implement it in Infrastructure or Persistence. Never implement without an interface.

---

### Rule 4 — Controllers Delegate, They Never Act

ASP.NET Core controllers in `ACLS.Api` are routing and HTTP-translation layers only.

A controller method must:
1. Receive an HTTP request.
2. Map the request to a MediatR command or query.
3. Send it via `IMediator.Send(...)`.
4. Map the result to an HTTP response.
5. Return the response.

A controller method must never:
- Write a LINQ query.
- Reference `DbContext` directly.
- Call a repository directly.
- Contain `if` statements implementing business rules.
- Perform calculations.
- Read `PropertyId` from the request body or query string.

Correct pattern:
```csharp
[HttpPost]
public async Task<IActionResult> AssignComplaint(
    [FromBody] AssignComplaintRequest request,
    CancellationToken ct)
{
    var command = new AssignComplaintCommand(request.ComplaintId, request.StaffId);
    var result = await _mediator.Send(command, ct);
    return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
}
```

---

### Rule 5 — PropertyId Is Always From the JWT, Never From the Request

Multi-tenancy is enforced by `PropertyId`. This value is extracted from the authenticated user's JWT claims by `TenancyMiddleware` and injected as `ICurrentPropertyContext`.

**You must never:**
- Accept `PropertyId` as a request body field.
- Accept `PropertyId` as a query string parameter.
- Accept `PropertyId` as a route parameter.
- Trust any client-supplied value for `PropertyId`.

**You must always:**
- Inject `ICurrentPropertyContext` into command handlers that need `PropertyId`.
- Read `currentPropertyContext.PropertyId` from the injected service.

Every repository query that touches a property-scoped entity must filter by `PropertyId`. No exceptions. A query that retrieves complaints without a `PropertyId` filter is a multi-tenancy data leak.

---

### Rule 6 — Binary Files Never Enter MSSQL

Media files (images, photos, completion evidence) are never stored in the database.

The mandatory workflow for any file upload:

```
1. Receive multipart form data in the controller.
2. Pass the file stream to IStorageService.UploadAsync(stream, fileName).
3. Receive the blob URL string back from IStorageService.
4. Create a Media entity with the URL string.
5. Save the Media entity (URL only) to the Media table in MSSQL.
```

A `Media` table row contains only: `mediaId`, `url` (string), `type` (string), `complaintId` (FK), `uploadedAt` (DateTime). It never contains a `byte[]`, `varbinary`, `nvarchar(MAX)` blob, or any binary content.

If you find yourself writing `byte[]` or `varbinary` in a model or migration, stop. You are violating this rule.

---

### Rule 7 — The Assign Complaint Transaction Is Atomic

When a complaint is assigned to a staff member, two writes must happen in a single database transaction. If either write fails, both must be rolled back.

```
Transaction {
    complaint.Status = ASSIGNED
    complaint.AssignedStaffId = staffId
    staff.Availability = BUSY
    staff.LastAssignedAt = DateTime.UtcNow
    Commit()
}
```

These two writes must never be separated into two independent calls. The system must never be in a state where a complaint is ASSIGNED but the staff member is still AVAILABLE, or where a staff member is BUSY but no complaint is ASSIGNED to them.

---

### Rule 8 — The Resolve Complaint Sequence Is Fixed

When a complaint is resolved, operations must happen in this exact order:

```
1. complaint.Status = RESOLVED
2. complaint.ResolvedAt = DateTime.UtcNow
3. staff.Availability = AVAILABLE
4. Commit transaction (steps 1–3 are atomic)
5. Publish ComplaintResolvedEvent
6. NotificationService notifies resident (triggered by event)
7. Enqueue async TAT calculation job to ACLS.Worker
```

Steps 1–4 are synchronous and transactional. Steps 5–7 are asynchronous and must not block the HTTP response.

---

### Rule 9 — The Smart Dispatch Algorithm Has a Fixed Formula

`IDispatchService.FindOptimalStaff` must implement this exact formula. Do not invent a different weighting or scoring method:

```
For each candidate staff member where availability == AVAILABLE and propertyId matches:

    skillScore    = count(intersection(staff.Skills, complaint.RequiredSkills))
                    ÷ count(complaint.RequiredSkills)
                    [0.0 if RequiredSkills is empty → 1.0 for all candidates]

    idleScore     = Normalise(DateTime.UtcNow - staff.LastAssignedAt)
                    [normalised to 0.0–1.0 against the max idle time in the candidate pool]

    urgencyWeight = complaint.Urgency == SOS_EMERGENCY ? 2.0 : 1.0

    matchScore    = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight

Return List<StaffScore> ordered by matchScore DESCENDING.
```

The return type is always a ranked list. The caller (manager or SOS handler) decides how many candidates to use. The dispatch service never makes assignment decisions — it only ranks.

---

### Rule 10 — Secrets Are Never Hardcoded

No connection string, API key, JWT signing secret, blob storage account key, or any credential of any kind may appear in committed source code.

| Secret type | Where it lives |
|---|---|
| Local development DB connection string | `appsettings.Development.json` (gitignored) |
| Production DB connection string | Azure Key Vault → environment variable `ACLS_DB_CONNECTION` |
| JWT signing key | Environment variable `ACLS_JWT_SECRET` |
| Blob storage connection | Environment variable `ACLS_STORAGE_CONNECTION` |
| Notification provider key | Environment variable `ACLS_NOTIFICATION_KEY` |

`appsettings.json` (committed) contains only empty string placeholders:
```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "Jwt": { "Secret": "", "Issuer": "acls-api", "Audience": "acls-clients" }
}
```

If you ever write an actual connection string or secret value in any `.cs`, `.json`, `.yaml`, `.env`, or `.yml` file that is not gitignored, stop. You are violating this rule.

---

## 4. File Placement Rules

### Backend

| What | Where |
|---|---|
| Entity classes | `ACLS.Domain/<BoundedContext>/` |
| Domain interfaces (repositories, services) | `ACLS.Domain/<BoundedContext>/` |
| Domain enums | `ACLS.Domain/<BoundedContext>/` |
| Domain events | `ACLS.Domain/<BoundedContext>/Events/` |
| Strongly-typed IDs, base classes, Result<T> | `ACLS.SharedKernel/` |
| MediatR commands and handlers | `ACLS.Application/<BoundedContext>/Commands/<CommandName>/` |
| MediatR queries and handlers | `ACLS.Application/<BoundedContext>/Queries/<QueryName>/` |
| Application-layer DTOs | `ACLS.Application/<BoundedContext>/DTOs/` |
| FluentValidation validators | `ACLS.Application/<BoundedContext>/Commands/<CommandName>/` |
| ASP.NET Core controllers | `ACLS.Api/Controllers/` |
| Middleware (auth, tenancy) | `ACLS.Api/Middleware/` |
| API request/response models | `ACLS.Contracts/` |
| EF Core DbContext | `ACLS.Persistence/` |
| Repository implementations | `ACLS.Persistence/<BoundedContext>/` |
| EF Core migrations | `ACLS.Persistence/Migrations/` |
| EF Core entity configurations | `ACLS.Persistence/Configurations/` |
| Service implementations (storage, notifications) | `ACLS.Infrastructure/<BoundedContext>/` |
| Background job handlers | `ACLS.Worker/Jobs/` |
| Unit tests | `tests/unit/Domain.Tests/` or `tests/unit/Application.Tests/` |
| Integration tests | `tests/integration/Persistence.Tests/` |
| Contract tests | `tests/contract/Api.ContractTests/` |
| E2E tests | `tests/e2e/Api.E2ETests/` |

### Frontend (React)

| What | Where |
|---|---|
| Next.js pages | `ResidentApp.Web/src/app/<route>/page.tsx` |
| Shared UI components | `ResidentApp.Web/src/components/ui/` |
| Feature components | `ResidentApp.Web/src/components/<feature>/` |
| API client calls | `ResidentApp.Web/src/lib/api/` |
| TypeScript types | `packages/api-contracts/` or `packages/shared-types/` |

### Frontend (Android — Kotlin)

| What | Where |
|---|---|
| Retrofit API interface | `app/src/main/java/com/acls/<app>/data/remote/ApiService.kt` |
| DTOs (mirrors ACLS.Contracts) | `app/src/main/java/com/acls/<app>/data/remote/dto/` |
| Repository implementations | `app/src/main/java/com/acls/<app>/data/repository/` |
| Domain models | `app/src/main/java/com/acls/<app>/domain/model/` |
| Repository interfaces | `app/src/main/java/com/acls/<app>/domain/repository/` |
| Composable screens + ViewModels | `app/src/main/java/com/acls/<app>/ui/screens/<ScreenName>/` |
| Shared Composables | `app/src/main/java/com/acls/<app>/ui/components/` |
| Hilt DI module | `app/src/main/java/com/acls/<app>/di/AppModule.kt` |

### Frontend (iOS — Swift)

| What | Where |
|---|---|
| API client | `Sources/Data/Network/APIClient.swift` |
| Codable DTOs | `Sources/Data/Network/DTOs/` |
| Repository implementations | `Sources/Data/Repositories/` |
| Domain models | `Sources/Domain/Models/` |
| Repository protocols | `Sources/Domain/Repositories/` |
| SwiftUI Views + ViewModels | `Sources/UI/Screens/<ScreenName>/` |
| Shared SwiftUI components | `Sources/UI/Components/` |

### Documentation

| What | Where |
|---|---|
| Project governance and ADRs | `docs/00_Project_Constitution/` |
| Product requirements and user stories | `docs/01_Product/` |
| Domain models and bounded contexts | `docs/02_Domain/` |
| Architecture diagrams and decisions | `docs/03_Architecture/` |
| Data model, schema, ERD | `docs/04_Data/` |
| API spec (OpenAPI) and error codes | `docs/05_API/` |
| UML diagrams (PlantUML/Mermaid source) | `docs/06_UML/` |
| Coding standards, patterns, testing strategy | `docs/07_Implementation/` |
| UX wireframes and user flows | `docs/08_UX/` |
| Runbooks and SLOs | `docs/09_Operations/` |
| Security and compliance | `docs/10_Security/` |

**No documentation file may live outside `docs/`.** Not in `backend/`, not in `apps/`, not in the repo root (except `README.md`).

---

## 5. Naming Conventions

### C# Backend

| Concept | Convention | Example |
|---|---|---|
| Projects | `ACLS.<Layer>` | `ACLS.Domain` |
| Namespaces | Match project + feature folder | `ACLS.Domain.Complaints` |
| Interfaces | Prefix `I` | `IComplaintRepository` |
| Entities | PascalCase noun | `Complaint`, `StaffMember` |
| Enums | PascalCase, values SCREAMING_SNAKE | `TicketStatus.IN_PROGRESS` |
| Commands | `<Verb><Noun>Command` | `AssignComplaintCommand` |
| Queries | `Get<Noun>Query` | `GetComplaintByIdQuery` |
| Handlers | Match command/query + `Handler` | `AssignComplaintCommandHandler` |
| DTOs | `<Noun>Dto` | `ComplaintDto`, `StaffScoreDto` |
| Controllers | `<Noun>Controller` | `ComplaintsController` |
| Middleware | `<Purpose>Middleware` | `TenancyMiddleware` |
| Events | `<Noun><PastTense>Event` | `ComplaintAssignedEvent` |

### TypeScript / React

| Concept | Convention | Example |
|---|---|---|
| Components | PascalCase | `ComplaintCard.tsx` |
| Hooks | `use` prefix, camelCase | `useComplaints.ts` |
| API functions | camelCase verb | `assignComplaint()` |
| Types/interfaces | PascalCase | `ComplaintDto`, `TicketStatus` |
| Pages (Next.js) | `page.tsx` in route folder | `complaints/[id]/page.tsx` |
| Enum values | SCREAMING_SNAKE (match backend) | `TicketStatus.IN_PROGRESS` |

### Kotlin (Android)

| Concept | Convention | Example |
|---|---|---|
| Data classes (DTOs) | PascalCase + `Dto` suffix | `ComplaintDto` |
| Composable functions | PascalCase | `ComplaintCard()` |
| ViewModels | PascalCase + `ViewModel` | `ComplaintsViewModel` |
| Repositories (interface) | `I` prefix + `Repository` | `IComplaintRepository` |
| Retrofit methods | camelCase verb | `assignComplaint()` |

### Swift (iOS)

| Concept | Convention | Example |
|---|---|---|
| Structs/classes | PascalCase | `ComplaintDto`, `StaffMember` |
| SwiftUI Views | PascalCase | `ComplaintCardView` |
| ViewModels | PascalCase + `ViewModel` | `ComplaintsViewModel` |
| Protocols | PascalCase (no `I` prefix — Swift convention) | `ComplaintRepository` |
| Functions | camelCase | `assignComplaint()` |

---

## 6. Bounded Context Ownership

Each bounded context owns its entities, its repository interface, and its events. No bounded context may directly query another bounded context's repository.

| Bounded context | Owns | Does NOT own |
|---|---|---|
| `Identity` | `User`, `InvitationToken`, `Role` | Complaints, Staff details |
| `Properties` | `Property`, `Building`, `Unit`, `PropertyId` | Users, Complaints |
| `Residents` | `Resident` (extends User) | Complaint details beyond FK |
| `Staff` | `StaffMember`, `StaffState` | Complaints beyond assignment FK |
| `Complaints` | `Complaint`, `Media`, `WorkNote`, `TicketStatus`, `Urgency` | Staff availability, Resident profile |
| `Dispatch` | `IDispatchService`, `StaffScore`, `DispatchCriteria` | Persistence — it reads via `IStaffRepository` |
| `Notifications` | `INotificationService`, `NotificationChannel` | Complaint content — receives events only |
| `Outages` | `Outage`, `OutageType` | Resident contact details — uses `INotificationService` |
| `AuditLog` | `AuditEntry`, `AuditAction` | Business logic — records only |
| `Reporting` | `IReportingService`, read models | Write operations — read only |

Cross-context communication happens via domain events published by the source context and handled by the destination context. Never via direct repository injection across context boundaries.

---

## 7. What to Do When Uncertain

If you are uncertain about any of the following, stop generating code and state your uncertainty explicitly:

- Where a file should be placed.
- Which layer owns a piece of logic.
- Whether a field should be in the database or derived.
- Whether `PropertyId` should be passed in a request.
- Whether a piece of logic belongs in the frontend or backend.
- Whether a test should be unit, integration, or E2E.
- Whether a new external dependency is appropriate.

State the uncertainty in the format:
```
UNCERTAINTY: [describe the decision point]
OPTIONS: [list 2–3 options]
RECOMMENDATION: [which option you would choose and why]
WAITING FOR: confirmation before proceeding
```

Do not make an assumption and proceed silently. A silent wrong assumption compounds across sessions.

---

## 8. What You May Not Do Without Explicit Instruction

The following actions require explicit approval in the session prompt before you may take them:

- Add a new NuGet package, npm package, or Gradle dependency.
- Create a new top-level directory in the repository.
- Add a new table or column to the database schema (beyond what the current phase specifies).
- Change the dispatch algorithm weights.
- Change the ticket status state machine (add or remove a status).
- Change an API endpoint URL, HTTP method, or response shape after it has been defined.
- Add a new bounded context not listed in the blueprint.
- Generate boilerplate for a phase not yet reached (e.g. generating iOS code during the backend phase).

---

## 9. Output Quality Standards

Every code artifact you produce must meet these standards:

**Completeness** — No `// TODO`, `// implement later`, or placeholder method bodies. If a method is stubbed, it must throw `NotImplementedException` with a comment explaining exactly what needs to be implemented and why it was deferred.

**Compilability** — C# code must compile without errors against .NET 8. TypeScript must compile without errors with strict mode enabled. Kotlin must compile with the project's configured Kotlin version.

**No magic strings** — Enum values, route paths, claim names, configuration keys, and error codes must be defined as constants or typed values. No hardcoded strings inline in logic.

**One responsibility per class** — Do not combine a command handler with a query handler. Do not combine multiple unrelated endpoints in a controller action. Do not combine domain logic with persistence logic in a single class.

**Tests accompany code** — When generating a service or algorithm (especially `DispatchService`, `ComplaintService` transaction methods), generate the corresponding NUnit test class in the same output. Tests are not deferred to a "testing phase" — they are written alongside the code they verify.

---

## 10. Phase Reference

| Phase | What gets built | Key documents to read |
|---|---|---|
| **Phase 1** | `ACLS.Domain` entities, `ACLS.Persistence` DbContext, repository interfaces, EF Core migrations | `data_model_overview.md`, `erd.md`, `clean_architecture_guide.md` |
| **Phase 2** | `DispatchService`, `ComplaintService`, NUnit tests | `dispatch_algorithm.md`, `multi_tenancy_pattern.md`, `testing_strategy.md` |
| **Phase 3** | ASP.NET Core controllers, DTOs, `ACLS.Contracts`, middleware | `api_overview.md`, `v1.yaml`, `media_upload_pattern.md` |
| **Phase 4a** | React + Next.js Manager Dashboard | `design_system.md`, `user_flows/`, `api_overview.md` |
| **Phase 4b** | Android apps (ResidentApp + StaffApp) | `api_overview.md`, `coding_standards.md` |
| **Phase 4c** | iOS apps (ResidentApp + StaffApp) | `api_overview.md`, `coding_standards.md` |
| **Phase 5** | `ACLS.Worker` background jobs, notification service implementation | `testing_strategy.md`, `observability.md` |
| **Phase 6** | Integration tests, E2E tests, contract tests | `testing_strategy.md` |
| **Phase 7** | Terraform infrastructure, CI/CD pipelines | `environment_config.md` |

---

*End of AI Collaboration Rules v1.0*
