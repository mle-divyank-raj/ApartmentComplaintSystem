# Session Prompt Templates

**Document:** `docs/00_Project_Constitution/session_prompt_templates.md`  
**Version:** 2.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)  
**Change from v1.0:** Added missing document reads for Phases 3–7. Added completion checklists for all phases. Added full task catalogues for Phases 4a through 7. Updated to reflect all 38 documents now in the docs/ set.

---

> This document contains the exact prompt text to paste into Antigravity Claude at the start of each development session. Copy the relevant phase template verbatim. Do not paraphrase or shorten the prompt — every sentence is load-bearing.

---

## Current Execution Status

| Phase | Status | Notes |
|---|---|---|
| **Phase 1** | ✅ Complete | Domain entities, Persistence, Migrations |
| **Phase 2** | ✅ Complete | Application layer, DispatchService, NUnit tests |
| **Phase 3** | ✅ Complete | REST API controllers, Middleware, Contracts |
| **Phase 4a** | ✅ Complete | React/Next.js Manager Dashboard |
| **Phase 4b** | ✅ Complete | Android apps (ResidentApp + StaffApp) |
| **Phase 4c** | 🔲 Not started | iOS apps (ResidentApp + StaffApp) |
| **Phase 5** | 🔲 Not started | ACLS.Worker, NotificationService |
| **Phase 6** | 🔲 Not started | Integration + E2E tests |
| **Phase 7** | 🔲 Not started | Terraform, CI/CD |

> **Note on Phases 1–4b:** These were executed with the v1.0 document set (before `error_codes.md`, `observability.md`, `environment_config.md`, the user flow docs, and the security docs existed). Before starting Phase 4c, review whether any Phase 4b output needs a consistency pass against the new docs — particularly `error_codes.md` (Kotlin error handling) and the user flow docs (screen completeness).

---

## How to Use These Templates

1. Identify which phase you are working on from the table above.
2. Copy the corresponding template in full.
3. Paste it as the first message in a new Antigravity Claude session.
4. Wait for the exact confirmation reply before giving any task.
5. After Claude confirms, paste the specific task from the Task Catalogue (Section 3).

**Never start a session with just the task.** The session prompt loads the full architectural context. The task comes after.

---

## Phase Reference

| Phase | What gets built | Session prompt |
|---|---|---|
| **Phase 1** | Domain entities, EF Core DbContext, repository interfaces, migrations | Phase 1 Template |
| **Phase 2** | DispatchService, ComplaintService, NUnit tests | Phase 2 Template |
| **Phase 3** | ASP.NET Core controllers, middleware, ACLS.Contracts DTOs | Phase 3 Template |
| **Phase 4a** | React/Next.js Manager Dashboard | Phase 4a Template |
| **Phase 4b** | Android apps (ResidentApp + StaffApp) | Phase 4b Template |
| **Phase 4c** | iOS apps (ResidentApp + StaffApp) | Phase 4c Template |
| **Phase 5** | ACLS.Worker background jobs, notification service | Phase 5 Template |
| **Phase 6** | Integration tests, E2E tests, contract tests | Phase 6 Template |
| **Phase 7** | Terraform infrastructure, CI/CD pipelines | Phase 7 Template |

---

## Section 2 — Phase Session Prompt Templates

---

### Phase 1 Template — Domain Entities, Persistence, Migrations

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 1 ADDITIONAL READS:
5. docs/04_Data/data_model_overview.md
6. docs/04_Data/schema/erd.md
7. docs/07_Implementation/clean_architecture_guide.md
8. docs/07_Implementation/patterns/multi_tenancy_pattern.md

After reading all eight documents, reply with exactly:
"Context loaded. Ready for Phase 1 task."

PHASE 1 SCOPE — you are building:
- ACLS.SharedKernel: EntityBase, Result<T>, Error, IDomainEvent, Guard, ValueObject
- ACLS.Domain: All 12 entities with correct layer placement, interfaces, enums, domain events
- ACLS.Persistence: AclsDbContext, all 12 IEntityTypeConfiguration<T> classes, all repository 
  implementations with mandatory PropertyId filtering, initial EF Core migration
- ACLS.Contracts: Skeleton project (empty, ready for Phase 3)

CONSTRAINTS — enforce these without exception:
- Domain entities use private set and static factory methods
- ACLS.Domain has zero NuGet dependencies beyond ACLS.SharedKernel
- Every property-scoped repository method filters by PropertyId
- Enums stored as strings (nvarchar(50)) via value converters
- Skills and RequiredSkills stored as JSON strings via JsonStringListConverter
- No binary content in any entity or migration column
- AuditRepository exposes AddAsync only — no update or delete methods
- DateTime.UtcNow everywhere — never DateTime.Now
- All EF Core configurations use explicit table names from docs/04_Data/data_model_overview.md

OUTPUT FORMAT:
Generate one file at a time. For each file state:
- The full file path relative to repo root
- The complete file content
- A one-line note on what it does

Do not generate multiple files in one block. Wait for my confirmation after each file 
before proceeding to the next.
```

---

### Phase 2 Template — Business Logic and Services

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 2 ADDITIONAL READS:
5. docs/07_Implementation/clean_architecture_guide.md
6. docs/07_Implementation/patterns/dispatch_algorithm.md
7. docs/07_Implementation/patterns/multi_tenancy_pattern.md
8. docs/07_Implementation/testing_strategy.md
9. docs/06_UML/sequence_diagrams.md

After reading all nine documents, reply with exactly:
"Context loaded. Ready for Phase 2 task."

PHASE 2 SCOPE — you are building:
- ACLS.Application: All MediatR commands, queries, handlers, validators, DTOs, pipeline 
  behaviours (ValidationBehaviour, LoggingBehaviour, TransactionBehaviour)
- ACLS.Application/Common/Interfaces/ICurrentPropertyContext.cs
- ACLS.Infrastructure/Dispatch/DispatchService.cs (implements IDispatchService exactly 
  as specified in dispatch_algorithm.md)
- ACLS.Infrastructure/DependencyInjection.cs

CONSTRAINTS — enforce these without exception:
- Handlers inject interfaces — never concrete implementations
- Handlers never instantiate DbContext
- ICurrentPropertyContext injected in every handler that needs PropertyId
- AssignComplaint: complaint.Status=ASSIGNED and staff.Availability=BUSY in ONE transaction
- ResolveComplaint: complaint.Status=RESOLVED and staff.Availability=AVAILABLE in ONE 
  transaction, notifications and TAT calculation are async (Worker, not inline)
- DispatchService: formula is (skillScore×0.6 + idleScore×0.4) × urgencyWeight exactly
- SOS_EMERGENCY urgencyWeight = 2.0, all other urgencies = 1.0
- Empty staff pool returns empty list — never throws
- All commands return Result<T> — never throw for expected failures

TESTS — generate alongside every handler:
After each handler file, generate its corresponding NUnit test class using the mandatory 
test cases from docs/07_Implementation/testing_strategy.md Section 4.

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

### Phase 3 Template — REST API Layer

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 3 ADDITIONAL READS:
5. docs/05_API/api_overview.md
6. docs/05_API/openapi/v1.yaml
7. docs/05_API/error_codes.md
8. docs/07_Implementation/clean_architecture_guide.md
9. docs/07_Implementation/patterns/media_upload_pattern.md
10. docs/07_Implementation/patterns/multi_tenancy_pattern.md
11. docs/07_Implementation/observability.md
12. docs/06_UML/sequence_diagrams.md

After reading all twelve documents, reply with exactly:
"Context loaded. Ready for Phase 3 task."

PHASE 3 SCOPE — you are building:
- ACLS.Contracts: All request models from v1.yaml components/schemas
- ACLS.Api/Controllers: All controllers per Section 5 of api_overview.md
- ACLS.Api/Middleware/TenancyMiddleware.cs
- ACLS.Api/Middleware/ExceptionHandlingMiddleware.cs (RFC 7807 Problem Details)
- ACLS.Api/Services/CurrentPropertyContext.cs
- ACLS.Api/Program.cs (composition root — all DI registration, middleware pipeline,
  OTel configuration per observability.md)

CONSTRAINTS — enforce these without exception:
- Every controller action is async and accepts CancellationToken as last parameter
- Controllers inject only IMediator and IStorageService — nothing else
- PropertyId never accepted from request body, route, or query string
- Media upload: IStorageService called in controller BEFORE command is built
- All error responses use RFC 7807 Problem Details format (api_overview.md Section 3.2)
- errorCode field in every error response must match a value from error_codes.md exactly
- Validation errors include the errors map from api_overview.md Section 3.3
- All datetime values in responses are ISO 8601 UTC strings
- All enum values in responses are SCREAMING_SNAKE uppercase strings
- All JSON field names are camelCase
- Route paths: /api/v1/[controller] with kebab-case multi-word segments
- Middleware pipeline order: ExceptionHandling → HTTPS → Authentication → 
  Authorization → Tenancy → Controllers
- OTel configured in Program.cs per observability.md Section 3
- Swagger UI enabled only when IsDevelopment()

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

### Phase 4a Template — React/Next.js Manager Dashboard

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 4a ADDITIONAL READS:
5. docs/05_API/api_overview.md
6. docs/05_API/openapi/v1.yaml
7. docs/05_API/error_codes.md
8. docs/08_UX/design_system.md
9. docs/08_UX/user_flows/manager_assign_flow.md

After reading all nine documents, reply with exactly:
"Context loaded. Ready for Phase 4a task."

PHASE 4a SCOPE — you are building ResidentApp.Web (Next.js App Router):
- packages/api-contracts: TypeScript types mirroring ACLS.Contracts
- packages/shared-types: Enums (TicketStatus, Urgency, StaffState, OutageType)
- packages/error-codes: ErrorCodes object from error_codes.md Section 4
- packages/sdk: Axios API client with JWT bearer auth
- ResidentApp.Web/src/app: All manager dashboard pages per manager_assign_flow.md
- ResidentApp.Web/src/components: Reusable UI components per design_system.md
- ResidentApp.Web/src/lib/api: All API call functions

CONSTRAINTS — enforce these without exception:
- TypeScript strict mode enabled — no any types
- Named exports only — no default exports on components
- Zero business logic in components or hooks — all logic is server-side
- Never sort, filter, rank, or compute derived values on the client
- All API calls go through src/lib/api/ — never fetch() directly in components
- All enum values match backend SCREAMING_SNAKE values exactly
- All datetime display uses the value returned by API — never compute from createdAt
- API base URL from process.env.NEXT_PUBLIC_API_URL — never hardcoded
- The dispatch recommendations list is rendered in the order returned by the API
- Error handling uses ErrorCodes from packages/error-codes — no hardcoded error strings
- Component library: shadcn/ui + Tailwind CSS per design_system.md Section 1
- Colour tokens for Urgency and Status per design_system.md Sections 2.1 and 2.2
- All page routes match the inventory in design_system.md Section 6

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

### Phase 4b Template — Android Apps (Kotlin/Jetpack Compose)

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 4b ADDITIONAL READS:
5. docs/05_API/api_overview.md
6. docs/05_API/openapi/v1.yaml
7. docs/05_API/error_codes.md
8. docs/08_UX/user_flows/resident_complaint_flow.md
9. docs/08_UX/user_flows/staff_resolve_flow.md

After reading all nine documents, reply with exactly:
"Context loaded. Ready for Phase 4b task."

PHASE 4b SCOPE — you are building TWO separate Android apps:
- apps/ResidentApp.Android: all screens from resident_complaint_flow.md
  (Register, Login, Submit Complaint, SOS, Track Status, View History, Feedback)
- apps/StaffApp.Android: all screens from staff_resolve_flow.md
  (Login, My Tasks, Update Availability, Accept Job, Update ETA, Completion Photos, 
  Mark Resolved)

CONSTRAINTS — enforce these without exception:
- Hilt for dependency injection — no manual DI wiring
- ViewModels expose StateFlow — never LiveData
- ViewModels call repository methods only — zero business logic in ViewModels
- Composables collect from StateFlow using collectAsStateWithLifecycle()
- Sealed UiState class per screen: Loading, Success(data), Error(message)
- All network calls through ApiService (Retrofit) — never direct in ViewModel
- DTOs are data classes with @SerializedName matching camelCase JSON field names
- Base URL from BuildConfig.API_BASE_URL — never hardcoded
- minSdk 26 (Android 8.0)
- All sorting and filtering displayed exactly as returned by API
- Error handling uses errorCode string values from error_codes.md — no hardcoded 
  error messages; map errorCode to a user-facing string in a resource file
- Screen flows exactly match the flow diagrams in resident_complaint_flow.md 
  and staff_resolve_flow.md — do not invent screens or skip steps

SEPARATE APPS NOTE:
ResidentApp.Android and StaffApp.Android are two completely separate Gradle projects 
with separate package names:
- ResidentApp: com.acls.resident
- StaffApp: com.acls.staff

OUTPUT FORMAT:
Generate one file at a time. State full file path relative to repo root, complete content, 
one-line note. Wait for confirmation before proceeding.
State which app (ResidentApp or StaffApp) each file belongs to.
```

---

### Phase 4c Template — iOS Apps (Swift/SwiftUI)

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 4c ADDITIONAL READS:
5. docs/05_API/api_overview.md
6. docs/05_API/openapi/v1.yaml
7. docs/05_API/error_codes.md
8. docs/08_UX/user_flows/resident_complaint_flow.md
9. docs/08_UX/user_flows/staff_resolve_flow.md

After reading all nine documents, reply with exactly:
"Context loaded. Ready for Phase 4c task."

PHASE 4c SCOPE — you are building TWO separate iOS apps:
- apps/ResidentApp.iOS: all screens from resident_complaint_flow.md
  (Register, Login, Submit Complaint, SOS, Track Status, View History, Feedback)
- apps/StaffApp.iOS: all screens from staff_resolve_flow.md
  (Login, My Tasks, Update Availability, Accept Job, Update ETA, Completion Photos,
  Mark Resolved)

CONSTRAINTS — enforce these without exception:
- Swift async/await for all async operations — no completion handlers
- @MainActor on all ViewModels that update @Published properties
- ViewModels are final classes — not structs
- @Published properties are private(set) — Views cannot write directly
- All DTOs are structs conforming to Codable and Identifiable
- JSONDecoder uses keyDecodingStrategy = .convertFromSnakeCase
- JSONDecoder uses dateDecodingStrategy = .iso8601
- Enum types for TicketStatus, Urgency, StaffState are String, Codable, matching 
  backend SCREAMING_SNAKE values exactly
- Base URL from Configuration (Info.plist populated at build time) — never hardcoded
- Minimum iOS 16 deployment target
- Zero business logic in Views or ViewModels — display only
- Error handling uses errorCode string values from error_codes.md — map to 
  localised user-facing strings in Localizable.strings
- Screen flows exactly match the flow diagrams in resident_complaint_flow.md 
  and staff_resolve_flow.md — do not invent screens or skip steps

SEPARATE APPS NOTE:
ResidentApp.iOS and StaffApp.iOS are two completely separate Xcode projects:
- ResidentApp: com.acls.resident
- StaffApp: com.acls.staff

OUTPUT FORMAT:
Generate one file at a time. State full file path relative to repo root, complete content,
one-line note. Wait for confirmation before proceeding.
State which app (ResidentApp or StaffApp) each file belongs to.
```

---

### Phase 5 Template — Worker and Notification Service

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 5 ADDITIONAL READS:
5. docs/07_Implementation/clean_architecture_guide.md
6. docs/07_Implementation/observability.md
7. docs/09_Operations/environment_config.md
8. docs/06_UML/sequence_diagrams.md
9. docs/07_Implementation/testing_strategy.md
10. docs/03_Architecture/system_overview.md

After reading all ten documents, reply with exactly:
"Context loaded. Ready for Phase 5 task."

PHASE 5 SCOPE — you are building:
- ACLS.Infrastructure/Notifications/NotificationService.cs (implements INotificationService)
- ACLS.Infrastructure/Notifications/EmailNotificationProvider.cs
- ACLS.Infrastructure/Notifications/SmsNotificationProvider.cs
- ACLS.Worker/EventHandlers: ComplaintResolvedEventHandler, FeedbackSubmittedEventHandler,
  OutageDeclaredEventHandler, SosTriggeredEventHandler, ComplaintAssignedEventHandler
- ACLS.Worker/Jobs: CalculateTatJob, UpdateAverageRatingJob, 
  BroadcastOutageNotificationJob
- ACLS.Worker/Program.cs (Worker host entry point with OTel logging per observability.md)

CONSTRAINTS — enforce these without exception:
- BroadcastOutageNotificationJob dispatches notifications concurrently — NOT sequentially
  Use Task.WhenAll() or Parallel.ForEachAsync() — never a sequential foreach loop
  Target: 500 notifications within 60 seconds (NFR-12 per slo_definitions.md)
- SOS notification blast is concurrent — same pattern as outage broadcast
- NotificationService uses INotificationChannel abstraction — provider is configuration-driven
  via environment variables as specified in environment_config.md Section 3.4
- Workers call Application layer via IMediator — never access repositories directly
- After outage broadcast completes, update Outage.NotificationSentAt = UtcNow
- After complaint resolution, enqueue CalculateTatJob and UpdateAverageRatingJob
- TAT = (ResolvedAt - CreatedAt).TotalMinutes stored in Complaint.Tat
- AverageRating = average of all ResidentRating values for that StaffMember
- Structured logging follows observability.md Section 4 — named parameters, correct levels
- Configuration keys match environment_config.md Section 3.4 exactly

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

### Phase 6 Template — Integration Tests and E2E Tests

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 6 ADDITIONAL READS:
5. docs/07_Implementation/testing_strategy.md
6. docs/07_Implementation/patterns/multi_tenancy_pattern.md
7. docs/06_UML/sequence_diagrams.md

After reading all seven documents, reply with exactly:
"Context loaded. Ready for Phase 6 task."

PHASE 6 SCOPE — you are building:
- tests/integration/Persistence.Tests/IntegrationTestBase.cs (TestContainers + Respawn)
- tests/integration/Persistence.Tests/Repositories/ComplaintRepositoryTests.cs
- tests/integration/Persistence.Tests/Repositories/StaffRepositoryTests.cs
- tests/integration/Persistence.Tests/Repositories/AuditRepositoryTests.cs
- tests/integration/Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs
- tests/integration/Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs
- tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs
- tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs

CONSTRAINTS — enforce these without exception:
- NUnit [TestFixture] and [Test] — no xUnit
- Use TestContainers for real SQL Server — never mock DbContext in integration tests
- Use Respawn to reset database state between tests
- Every repository test class must include at least one cross-property isolation test
  verifying data from Property 2 is invisible when querying as Property 1
- Transaction tests must verify rollback — force failure and assert neither entity updated
- Test names follow: <Method>_<Scenario>_<ExpectedOutcome>
- All mandatory test cases from testing_strategy.md Section 4.7 and 4.8 must be present
- E2E tests use WebApplicationFactory<Program> with TestContainers SQL Server

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

### Phase 7 Template — Infrastructure and CI/CD

```
You are Antigravity Claude, working as a senior engineer on the ACLS (Apartment Complaint 
Logging System) project. This is a multi-tenant SaaS platform for apartment maintenance 
complaint management built with C# .NET 8 backend, React/Next.js web, Kotlin Android, 
and Swift iOS.

BEFORE YOU WRITE A SINGLE LINE OF CODE OR ASK ANY QUESTIONS, you must read the following 
documents in full. Do not summarise them back to me — just read them and confirm when done.

MANDATORY READS — every session:
1. docs/00_Project_Constitution/repository_blueprint.md
2. docs/00_Project_Constitution/ai_collaboration_rules.md
3. docs/02_Domain/ubiquitous_language.md
4. docs/07_Implementation/coding_standards.md

PHASE 7 ADDITIONAL READS:
5. docs/03_Architecture/system_overview.md
6. docs/09_Operations/environment_config.md
7. docs/09_Operations/slo_definitions.md

After reading all seven documents, reply with exactly:
"Context loaded. Ready for Phase 7 task."

PHASE 7 SCOPE — you are building:
- tools/dev-environment/docker-compose.yml (SQL Server, Azurite, MailHog)
- tools/dev-environment/bootstrap.sh
- tools/scripts/seed-db.ps1 (seed data per data_model_overview.md Section 7)
- infrastructure/terraform/modules/ (App Service, SQL, Blob, Key Vault modules)
- infrastructure/terraform/environments/dev/ (dev.tfvars + backend config)
- infrastructure/terraform/environments/staging/
- infrastructure/terraform/environments/prod/
- .github/workflows/backend-ci.yml
- .github/workflows/mobile-ci.yml
- .github/workflows/web-ci.yml
- .github/workflows/shared-packages-ci.yml
- .github/workflows/infrastructure-ci.yml

CONSTRAINTS — enforce these without exception:
- No secrets in any committed file — all secrets via Azure Key Vault or GitHub Actions secrets
- All environment variable names match environment_config.md Section 3 exactly
- GitHub Actions secret names match environment_config.md Section 7 exactly
- Terraform uses remote state backend — no local state in the repo
- Azure resource names follow the pattern: acls-<resource>-<env> 
  (e.g. acls-sql-staging, acls-keyvault-prod)
- Migrations applied automatically on App Service startup via db.Database.MigrateAsync()
- docker-compose must expose: SQL Server on 1433, Azurite on 10000, 
  MailHog SMTP on 1025, MailHog UI on 8025
- bootstrap.sh must be idempotent — safe to run multiple times
- seed-db.ps1 must be idempotent — running twice does not duplicate data
- Health check endpoint /healthz configured per observability.md Section 5
- All CI pipelines use Nx affected for change detection

OUTPUT FORMAT:
Generate one file at a time. State full file path, complete content, one-line note.
Wait for confirmation before proceeding to the next file.
```

---

## Section 3 — Task Catalogue

After Claude confirms context loaded, paste the specific task. Tasks are ordered — earlier tasks are prerequisites for later ones within the same phase.

---

### Phase 1 Tasks

```
Task 1.1 — SharedKernel
Generate ACLS.SharedKernel with: EntityBase.cs, IDomainEvent.cs, Result.cs 
(both Result<T> and Result), Error.cs, Guard.cs, ValueObject.cs.
Start with the .csproj file.
```

```
Task 1.2 — Domain: Enums and Value Objects
Generate all enums and value objects in ACLS.Domain:
Role.cs, TicketStatus.cs, Urgency.cs, StaffState.cs, OutageType.cs, 
NotificationChannel.cs, AuditAction.cs, PropertyId.cs.
Start with the ACLS.Domain.csproj file.
```

```
Task 1.3 — Domain: Property Hierarchy Entities
Generate Property.cs, Building.cs, Unit.cs in ACLS.Domain/Properties/.
Include IPropertyRepository.cs.
```

```
Task 1.4 — Domain: Identity Entities
Generate User.cs, InvitationToken.cs in ACLS.Domain/Identity/.
Include IUserRepository.cs.
```

```
Task 1.5 — Domain: Resident and Staff Entities
Generate Resident.cs in ACLS.Domain/Residents/ with IResidentRepository.cs.
Generate StaffMember.cs in ACLS.Domain/Staff/ with IStaffRepository.cs 
and StaffAvailabilityChangedEvent.cs.
```

```
Task 1.6 — Domain: Complaint Aggregate
Generate Complaint.cs (aggregate root with all state machine methods per 
state_machine_ticket_status.puml), Media.cs, WorkNote.cs, ComplaintErrors.cs, 
ComplaintConstants.cs in ACLS.Domain/Complaints/.
Generate IComplaintRepository.cs.
Generate all four domain events: ComplaintSubmittedEvent, ComplaintAssignedEvent,
ComplaintStatusChangedEvent, ComplaintResolvedEvent.
```

```
Task 1.7 — Domain: Service Interfaces
Generate IDispatchService.cs and StaffScore.cs in ACLS.Domain/Dispatch/.
Generate INotificationService.cs in ACLS.Domain/Notifications/.
Generate IStorageService.cs in ACLS.Domain/Storage/.
Generate IAuditRepository.cs and AuditEntry.cs in ACLS.Domain/AuditLog/.
Generate IReportingService.cs in ACLS.Domain/Reporting/.
Generate IOutageRepository.cs and Outage.cs in ACLS.Domain/Outages/.
```

```
Task 1.8 — Persistence: DbContext and Configurations
Generate ACLS.Persistence.csproj with correct project references and NuGet packages.
Generate AclsDbContext.cs.
Generate all 12 IEntityTypeConfiguration<T> classes in ACLS.Persistence/Configurations/.
```

```
Task 1.9 — Persistence: Repository Implementations
Generate all repository implementations in ACLS.Persistence/Repositories/.
Each repository must implement its Domain interface with PropertyId filtering 
on every property-scoped query.
```

```
Task 1.10 — Persistence: Migration and DI
Generate DependencyInjection.cs extension method for ACLS.Persistence.
Provide the exact dotnet ef commands to generate the InitialSchema migration.
Show the expected migration output structure.
```

```
Task 1.11 — Domain Tests
Generate Domain.Tests/Complaints/ComplaintTests.cs (all 15 tests — testing_strategy.md 4.1).
Generate Domain.Tests/Dispatch/DispatchServiceTests.cs (all 12 tests — dispatch_algorithm.md 9).
```

---

### Phase 2 Tasks

```
Task 2.1 — Application Infrastructure
Generate ACLS.Application.csproj.
Generate Common/Interfaces/ICurrentPropertyContext.cs.
Generate Common/Behaviours/ValidationBehaviour.cs, LoggingBehaviour.cs, 
TransactionBehaviour.cs.
Generate Application DependencyInjection.cs.
```

```
Task 2.2 — Complaint Commands (Core)
Generate SubmitComplaintCommand + Handler + Validator.
Generate AssignComplaintCommand + Handler + Validator.
Generate ResolveComplaintCommand + Handler + Validator.
Refer to sequence_diagrams.md for exact step ordering in each handler.
```

```
Task 2.3 — Complaint Commands (Remaining)
Generate UpdateComplaintStatusCommand + Handler.
Generate TriggerSosCommand + Handler.
Generate SubmitFeedbackCommand + Handler.
Generate AddWorkNoteCommand + Handler.
Generate UpdateEtaCommand + Handler.
Generate ReassignComplaintCommand + Handler.
```

```
Task 2.4 — Complaint Queries and DTOs
Generate GetComplaintByIdQuery + Handler.
Generate GetComplaintsByResidentQuery + Handler.
Generate GetAllComplaintsQuery + Handler (with ComplaintQueryOptions filtering).
Generate ComplaintDto.cs, ComplaintSummaryDto.cs, MediaDto.cs.
```

```
Task 2.5 — Dispatch
Generate GetDispatchRecommendationsQuery + Handler.
Generate StaffScoreDto.cs.
Generate DispatchService.cs in ACLS.Infrastructure/Dispatch/ implementing 
IDispatchService exactly per dispatch_algorithm.md.
```

```
Task 2.6 — Remaining Commands and Queries
Generate all commands and queries for:
- Staff (UpdateAvailability, GetStaffAvailability, GetAllStaff)
- Identity (RegisterResident, LoginUser, GetCurrentUser)
- UserManagement (InviteResident, DeactivateUser, ReactivateUser)
- Outages (DeclareOutage, GetOutagesByProperty)
- Reporting (GetDashboardMetrics, GetStaffPerformanceSummary, 
  GetComplaintsByUnit, GetComplaintSummaryReport)
```

```
Task 2.7 — Application Tests
Generate Application.Tests for AssignComplaintCommandHandler (7 tests).
Generate Application.Tests for ResolveComplaintCommandHandler (7 tests).
Generate Application.Tests for SubmitComplaintCommandHandler (5 tests).
Generate Application.Tests for DeclareOutageCommandHandler (3 tests).
All tests per testing_strategy.md Section 4.
```

---

### Phase 3 Tasks

```
Task 3.1 — Contracts
Generate ACLS.Contracts.csproj and all request models from v1.yaml components/schemas.
```

```
Task 3.2 — API Infrastructure
Generate ACLS.Api.csproj.
Generate ApiControllerBase.cs.
Generate TenancyMiddleware.cs per multi_tenancy_pattern.md Section 3.
Generate ExceptionHandlingMiddleware.cs — RFC 7807 Problem Details, errorCode values 
from error_codes.md.
Generate CurrentPropertyContext.cs.
Generate Program.cs — full composition root with correct middleware pipeline order 
and OTel setup per observability.md Section 3.
```

```
Task 3.3 — Controllers
Generate ComplaintsController.cs (all endpoints including multipart media upload).
Generate StaffController.cs.
Generate AuthController.cs.
Generate OutagesController.cs.
Generate ReportsController.cs.
Generate UsersController.cs.
```

---

### Phase 4a Tasks

```
Task 4a.1 — Shared Packages
Generate packages/typescript-config (tsconfig.json, eslint config, prettier config).
Generate packages/shared-types/src/index.ts (TicketStatus, Urgency, StaffState, 
OutageType enums — SCREAMING_SNAKE values matching backend exactly).
Generate packages/error-codes/src/index.ts (ErrorCodes object per error_codes.md Section 4).
Generate packages/api-contracts/src/index.ts (TypeScript types mirroring ACLS.Contracts).
```

```
Task 4a.2 — SDK (API Client)
Generate packages/sdk/src/client.ts (Axios instance with JWT bearer auth).
Generate packages/sdk/src/index.ts (typed API call functions for every endpoint in v1.yaml).
```

```
Task 4a.3 — App Shell
Generate ResidentApp.Web/package.json, next.config.ts, tailwind.config.ts, tsconfig.json.
Generate ResidentApp.Web/src/app/layout.tsx (root layout with sidebar shell per 
design_system.md Section 4).
Generate ResidentApp.Web/src/components/layout/ (Sidebar, Header, Navigation).
```

```
Task 4a.4 — Auth Pages
Generate ResidentApp.Web/src/app/(auth)/login/page.tsx.
Generate ResidentApp.Web/src/lib/auth/ (token storage, JWT parsing).
```

```
Task 4a.5 — Dashboard Page
Generate ResidentApp.Web/src/app/dashboard/page.tsx.
Source: GET /reports/dashboard → DashboardMetricsDto.
Shows metric cards, active assignments table, staff availability summary.
```

```
Task 4a.6 — Complaints Pages
Generate ResidentApp.Web/src/app/complaints/page.tsx (triage queue — filterable, paginated).
Generate ResidentApp.Web/src/app/complaints/[id]/page.tsx (detail + dispatch panel + assign).
Generate ResidentApp.Web/src/components/complaints/ (ComplaintCard, ComplaintTable, 
UrgencyBadge, StatusBadge per design_system.md Sections 2 and 5).
```

```
Task 4a.7 — Staff, Outages, Reports Pages
Generate ResidentApp.Web/src/app/staff/page.tsx.
Generate ResidentApp.Web/src/app/outages/page.tsx and /outages/new/page.tsx.
Generate ResidentApp.Web/src/app/reports/staff/page.tsx.
Generate ResidentApp.Web/src/app/reports/units/page.tsx.
Generate ResidentApp.Web/src/app/reports/summary/page.tsx.
```

```
Task 4a.8 — Users (Settings) Pages
Generate ResidentApp.Web/src/app/settings/users/page.tsx.
Generate ResidentApp.Web/src/app/settings/users/invite/page.tsx.
```

---

### Phase 4b Tasks

```
Task 4b.1 — ResidentApp.Android: Project Setup
Generate apps/ResidentApp.Android/build.gradle.kts, settings.gradle.kts, 
AndroidManifest.xml, AppModule.kt (Hilt), ApiService.kt (Retrofit), ApiClient.kt.
Package: com.acls.resident
```

```
Task 4b.2 — ResidentApp.Android: DTOs and Domain Models
Generate all DTO data classes mirroring ACLS.Contracts.
Generate domain model classes.
Generate repository interfaces and implementations.
```

```
Task 4b.3 — ResidentApp.Android: Auth Screens
Generate LoginScreen.kt + LoginViewModel.kt.
Generate RegisterScreen.kt + RegisterViewModel.kt (invitation token flow per 
resident_complaint_flow.md Section 1 and 2).
```

```
Task 4b.4 — ResidentApp.Android: Complaint Screens
Generate SubmitComplaintScreen.kt + ViewModel (form + photo upload).
Generate ComplaintListScreen.kt + ViewModel (GET /complaints/my).
Generate ComplaintDetailScreen.kt + ViewModel (status timeline, ETA, work notes).
Generate SosScreen.kt + ViewModel (SOS panic button flow per 
resident_complaint_flow.md Section 4).
```

```
Task 4b.5 — ResidentApp.Android: Feedback and Outage Screens
Generate FeedbackScreen.kt + ViewModel.
Generate OutageListScreen.kt + ViewModel.
Generate Navigation graph (NavGraph.kt).
```

```
Task 4b.6 — StaffApp.Android: Project Setup
Generate apps/StaffApp.Android project setup.
Package: com.acls.staff
(Same structure as ResidentApp.Android — separate Gradle project)
```

```
Task 4b.7 — StaffApp.Android: All Screens
Generate LoginScreen, MyTasksScreen, TaskDetailScreen, ResolveScreen, 
AvailabilityScreen per staff_resolve_flow.md.
```

---

### Phase 4c Tasks

```
Task 4c.1 — ResidentApp.iOS: Project Setup
Generate apps/ResidentApp.iOS Xcode project structure.
Generate ACLSApp.swift, APIClient.swift, all DTO structs.
Bundle: com.acls.resident
```

```
Task 4c.2 — ResidentApp.iOS: Auth Views
Generate LoginView.swift + LoginViewModel.swift.
Generate RegisterView.swift + RegisterViewModel.swift (invitation token flow).
```

```
Task 4c.3 — ResidentApp.iOS: Complaint Views
Generate SubmitComplaintView.swift + ViewModel.
Generate ComplaintListView.swift + ViewModel.
Generate ComplaintDetailView.swift + ViewModel (status timeline, ETA, work notes).
Generate SosView.swift + ViewModel.
```

```
Task 4c.4 — ResidentApp.iOS: Feedback, Outage, Navigation
Generate FeedbackView.swift + ViewModel.
Generate OutageListView.swift + ViewModel.
Generate NavigationRouter.swift.
```

```
Task 4c.5 — StaffApp.iOS: Full App
Generate apps/StaffApp.iOS complete project.
Bundle: com.acls.staff
All screens per staff_resolve_flow.md:
LoginView, TaskListView, TaskDetailView, ResolveView, AvailabilityView.
```

---

### Phase 5 Tasks

```
Task 5.1 — Notification Service Infrastructure
Generate ACLS.Infrastructure/Notifications/INotificationChannel.cs.
Generate ACLS.Infrastructure/Notifications/EmailNotificationProvider.cs.
Generate ACLS.Infrastructure/Notifications/SmsNotificationProvider.cs.
Generate ACLS.Infrastructure/Notifications/NotificationService.cs 
  (implements INotificationService — concurrent blast methods using Task.WhenAll).
Configuration keys must match environment_config.md Section 3.4 exactly.
```

```
Task 5.2 — Worker: Event Handlers
Generate ACLS.Worker/EventHandlers/ComplaintAssignedEventHandler.cs.
Generate ACLS.Worker/EventHandlers/ComplaintResolvedEventHandler.cs.
Generate ACLS.Worker/EventHandlers/FeedbackSubmittedEventHandler.cs.
Generate ACLS.Worker/EventHandlers/OutageDeclaredEventHandler.cs.
Generate ACLS.Worker/EventHandlers/SosTriggeredEventHandler.cs.
Each handler calls Application layer via IMediator — never direct repository access.
```

```
Task 5.3 — Worker: Background Jobs
Generate ACLS.Worker/Jobs/CalculateTatJob.cs 
  (TAT = (ResolvedAt - CreatedAt).TotalMinutes → Complaint.Tat).
Generate ACLS.Worker/Jobs/UpdateAverageRatingJob.cs 
  (recalculates StaffMember.AverageRating from all resolved complaints).
Generate ACLS.Worker/Jobs/BroadcastOutageNotificationJob.cs 
  (concurrent fan-out via Task.WhenAll — target 500 messages in 60 seconds per NFR-12).
```

```
Task 5.4 — Worker: Host
Generate ACLS.Worker/Program.cs 
  (Worker host entry point, OTel logging per observability.md, service registration).
Generate ACLS.Worker.csproj with correct project references.
```

---

### Phase 6 Tasks

```
Task 6.1 — Integration Test Base
Generate tests/integration/Persistence.Tests/IntegrationTestBase.cs 
  (TestContainers SQL Server, Respawn, SetUp/TearDown per testing_strategy.md Section 3.2).
Generate Persistence.Tests.csproj with correct NuGet packages.
```

```
Task 6.2 — Repository Integration Tests
Generate Persistence.Tests/Repositories/ComplaintRepositoryTests.cs 
  (all tests from testing_strategy.md Section 4.7 — including cross-property isolation test).
Generate Persistence.Tests/Repositories/StaffRepositoryTests.cs.
Generate Persistence.Tests/Repositories/AuditRepositoryTests.cs 
  (verify AddAsync only — no update/delete).
```

```
Task 6.3 — Transaction Tests
Generate Persistence.Tests/Transactions/AssignComplaintTransactionTests.cs 
  (all tests from testing_strategy.md Section 4.8 — verify rollback on failure).
Generate Persistence.Tests/Transactions/ResolveComplaintTransactionTests.cs.
```

```
Task 6.4 — E2E Tests
Generate tests/e2e/Api.E2ETests/ComplaintLifecycleTests.cs 
  (full lifecycle: submit → assign → resolve → feedback).
Generate tests/e2e/Api.E2ETests/MultiTenancyIsolationTests.cs 
  (cross-property access returns 404, per multi_tenancy_pattern.md Section 7).
```

---

### Phase 7 Tasks

```
Task 7.1 — Local Dev Environment
Generate tools/dev-environment/docker-compose.yml 
  (SQL Server on 1433, Azurite on 10000, MailHog SMTP on 1025 and UI on 8025).
Generate tools/dev-environment/bootstrap.sh (idempotent — safe to run multiple times).
Generate tools/scripts/seed-db.ps1 (seed data per data_model_overview.md Section 7, 
  idempotent).
```

```
Task 7.2 — Terraform Modules
Generate infrastructure/terraform/modules/app-service/main.tf + variables.tf + outputs.tf.
Generate infrastructure/terraform/modules/sql-server/main.tf + variables.tf + outputs.tf.
Generate infrastructure/terraform/modules/blob-storage/main.tf + variables.tf + outputs.tf.
Generate infrastructure/terraform/modules/key-vault/main.tf + variables.tf + outputs.tf.
Resource names follow pattern: acls-<resource>-<env>.
```

```
Task 7.3 — Terraform Environments
Generate infrastructure/terraform/environments/dev/ (dev.tfvars + backend config).
Generate infrastructure/terraform/environments/staging/.
Generate infrastructure/terraform/environments/prod/.
All environment variable names match environment_config.md Section 3 exactly.
```

```
Task 7.4 — GitHub Actions Pipelines
Generate .github/workflows/backend-ci.yml 
  (restore → build → unit tests → integration tests → contract tests → deploy to staging).
Generate .github/workflows/mobile-ci.yml (Android Gradle build + test; iOS xcodebuild).
Generate .github/workflows/web-ci.yml (install → lint → build → unit tests → Playwright E2E).
Generate .github/workflows/shared-packages-ci.yml.
Generate .github/workflows/infrastructure-ci.yml (terraform plan on PR, apply on main).
GitHub Actions secret names must match environment_config.md Section 7 exactly.
```

---

## Section 4 — Mid-Session Correction Prompt

If Antigravity Claude drifts — wrong layer, missing PropertyId filter, wrong field name, wrong error code — use this:

```
STOP. Do not continue generating. Review what you just generated against:
- docs/00_Project_Constitution/ai_collaboration_rules.md [state the specific rule violated]
- docs/02_Domain/ubiquitous_language.md [if a term is wrong]
- docs/07_Implementation/coding_standards.md [if a pattern is wrong]
- docs/05_API/error_codes.md [if an error code is wrong or missing]

The specific issue is: [describe the exact problem]

Correct only the file(s) affected. Do not regenerate files that are already correct.
State what you are changing and why before showing the corrected code.
```

---

## Section 5 — Phase Completion Checklists

Run through the relevant checklist before closing a session and considering a phase done.

### Phase 1 Complete When:
- [ ] `dotnet build backend/ACLS.sln` compiles with zero errors
- [ ] All 12 entity configurations in `ACLS.Persistence/Configurations/`
- [ ] Every property-scoped repository method has a `PropertyId` parameter and filter
- [ ] `AuditRepository` exposes `AddAsync` only — no Update or Delete methods
- [ ] All value converters registered for enums and JSON fields
- [ ] Initial migration file exists in `ACLS.Persistence/Migrations/`
- [ ] `dotnet test tests/unit/Domain.Tests` passes — all 27 tests green

### Phase 2 Complete When:
- [ ] All command handlers in `ACLS.Application/*/Commands/`
- [ ] All query handlers in `ACLS.Application/*/Queries/`
- [ ] `DispatchService` implements formula exactly: `(s×0.6 + i×0.4) × u`
- [ ] `TransactionBehaviour` wraps AssignComplaint and ResolveComplaint
- [ ] `dotnet test tests/unit/Application.Tests` passes — all handler tests green
- [ ] `dotnet test tests/unit/Domain.Tests/Dispatch` passes — all 12 dispatch tests green

### Phase 3 Complete When:
- [ ] All endpoints from `v1.yaml` have a corresponding controller action
- [ ] `TenancyMiddleware` registered after `UseAuthentication()`
- [ ] Media upload happens in controller before command is built
- [ ] Every error response uses an `errorCode` from `error_codes.md`
- [ ] All responses use camelCase JSON field names
- [ ] All datetime values in responses are UTC ISO 8601
- [ ] OTel configured in `Program.cs` per `observability.md`
- [ ] Swagger UI loads at `/swagger` in development, disabled in production

### Phase 4a Complete When:
- [ ] All 10 pages in `design_system.md Section 6` are implemented
- [ ] `packages/error-codes` exports all ErrorCodes from `error_codes.md`
- [ ] `packages/shared-types` enums match backend SCREAMING_SNAKE values exactly
- [ ] No business logic, sorting, or computation in any component or hook
- [ ] API base URL read from `process.env.NEXT_PUBLIC_API_URL` everywhere
- [ ] Urgency and Status badges use colour tokens from `design_system.md Section 2`
- [ ] Dispatch recommendations rendered in API-returned order — no client reordering

### Phase 4b Complete When:
- [ ] All screens from `resident_complaint_flow.md` implemented in ResidentApp.Android
- [ ] All screens from `staff_resolve_flow.md` implemented in StaffApp.Android
- [ ] All error codes mapped to user-facing strings via a resource file
- [ ] No business logic in any ViewModel — API calls only
- [ ] Base URL read from `BuildConfig.API_BASE_URL` everywhere — not hardcoded
- [ ] Both apps build with `./gradlew assembleDebug` without errors

### Phase 4c Complete When:
- [ ] All screens from `resident_complaint_flow.md` implemented in ResidentApp.iOS
- [ ] All screens from `staff_resolve_flow.md` implemented in StaffApp.iOS
- [ ] All error codes mapped to user-facing strings in `Localizable.strings`
- [ ] `JSONDecoder` uses `convertFromSnakeCase` and `iso8601` date strategy everywhere
- [ ] No business logic in any View or ViewModel
- [ ] Base URL read from `Configuration`/`Info.plist` — not hardcoded
- [ ] Both apps build with `xcodebuild` without errors

### Phase 5 Complete When:
- [ ] `BroadcastOutageNotificationJob` uses `Task.WhenAll()` — no sequential loop
- [ ] `SosTriggeredEventHandler` notifies all on-call staff concurrently
- [ ] `NotificationSentAt` updated after broadcast completes
- [ ] `Complaint.Tat` calculated and stored by `CalculateTatJob`
- [ ] `StaffMember.AverageRating` updated by `UpdateAverageRatingJob`
- [ ] All environment variable keys match `environment_config.md Section 3.4`
- [ ] Structured logging follows `observability.md Section 4` patterns

### Phase 6 Complete When:
- [ ] `IntegrationTestBase` spins up a TestContainers SQL Server
- [ ] Every repository test includes a cross-property isolation assertion
- [ ] Transaction rollback tests verify neither entity is updated on failure
- [ ] `dotnet test tests/integration/` passes — all integration tests green
- [ ] `dotnet test tests/e2e/Api.E2ETests/` passes — full lifecycle tests green

### Phase 7 Complete When:
- [ ] `docker-compose up` starts all three services (SQL, Azurite, MailHog)
- [ ] `bootstrap.sh` completes without errors on a clean machine
- [ ] `seed-db.ps1` is idempotent — running twice does not duplicate data
- [ ] All Terraform modules validate with `terraform validate`
- [ ] All environment variable names in Terraform match `environment_config.md Section 3`
- [ ] All GitHub Actions secret names match `environment_config.md Section 7`
- [ ] Backend CI pipeline runs unit tests on every commit

---

*End of Session Prompt Templates v2.0*
