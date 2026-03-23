# System Overview

**Document:** `docs/03_Architecture/system_overview.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document describes the complete runtime architecture of ACLS — how components are deployed, how they communicate, and what each boundary enforces. Read this before proposing any new service boundary, infrastructure component, or cross-cutting concern. If a technology choice or deployment decision is not described here, raise it as an ADR before implementing it.

---

## 1. System Context

ACLS is a multi-tenant SaaS platform. It serves three distinct user populations across a structured property hierarchy. All data is scoped to a `Property` — no user can access data from a `Property` they are not authenticated against.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        External Actors                              │
│                                                                     │
│  [Resident]          [Property Manager]       [Maintenance Staff]   │
│  Mobile App          Web Dashboard             Mobile App           │
│  (Android/iOS)       (Browser)                 (Android/iOS)        │
└──────────┬──────────────────┬──────────────────────┬───────────────┘
           │  HTTPS/REST      │  HTTPS/REST           │  HTTPS/REST
           ▼                  ▼                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     System Boundary: ACLS                           │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    ACLS REST API                            │    │
│  │              (ASP.NET Core, Azure App Service)              │    │
│  └──────────────┬──────────────────────────┬───────────────────┘    │
│                 │ SQL/TCP                  │ AMQP / async           │
│  ┌──────────────▼──────────┐   ┌──────────▼────────────────────┐   │
│  │    Azure SQL Database   │   │        ACLS Worker            │   │
│  │    (MSSQL — primary     │   │  (Background jobs, async      │   │
│  │     relational store)   │   │   notification fan-out)       │   │
│  └─────────────────────────┘   └──────────┬────────────────────┘   │
│                                           │ HTTPS                  │
│  ┌────────────────────────────────────────▼────────────────────┐    │
│  │               External Services                             │    │
│  │  Azure Blob Storage   Email Provider   SMS Provider         │    │
│  │  (media files)        (INotification)  (INotification)      │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Container Architecture

ACLS consists of four runtime containers and three external services.

### 2.1 Runtime Containers

#### Container 1: ACLS REST API (`ACLS.Api`)

| Attribute | Value |
|---|---|
| **Technology** | ASP.NET Core 8, .NET 8 |
| **Hosting** | Azure App Service (Linux) |
| **Entry point** | `ACLS.Api/Program.cs` |
| **Listens on** | HTTPS port 443 (production), HTTP port 5000 (local dev) |
| **Auth** | JWT Bearer tokens, validated on every authenticated request |
| **Tenancy** | `TenancyMiddleware` enforces `PropertyId` scoping per request |

Responsibilities:
- Receive and route HTTP requests from all three client types
- Authenticate and authorise via JWT
- Enforce multi-tenancy via `PropertyId` claim extraction
- Dispatch commands and queries to `ACLS.Application` via MediatR
- Return HTTP responses per the contract in `docs/05_API/openapi/v1.yaml`
- Upload media files to Azure Blob Storage via `IStorageService` before persisting URLs

Does NOT:
- Execute business logic directly
- Query the database directly
- Send notifications directly
- Know about any specific storage or notification provider

#### Container 2: ACLS Worker (`ACLS.Worker`)

| Attribute | Value |
|---|---|
| **Technology** | .NET 8 Worker Service (`IHostedService`) |
| **Hosting** | Azure App Service (same App Service Plan as API, separate process) or Azure Container Apps |
| **Triggered by** | MediatR domain events published by `ACLS.Api` after committed transactions |

Responsibilities:
- Handle `ComplaintResolvedEvent` → calculate TAT, update `Complaint.Tat`
- Handle `FeedbackSubmittedEvent` → recalculate `StaffMember.AverageRating`
- Handle `OutageDeclaredEvent` → fan out mass SMS/email broadcast to all property residents (NFR-12: 500 messages within 60 seconds)
- Handle `ComplaintAssignedEvent` → notify assigned staff member
- Handle `SosTriggeredEvent` → simultaneously notify all on-call staff

Does NOT:
- Handle HTTP requests
- Expose any API surface
- Block the `ACLS.Api` response cycle

#### Container 3: Azure SQL Database

| Attribute | Value |
|---|---|
| **Technology** | Azure SQL Database (SQL Server compatibility level 150+) |
| **Accessed by** | `ACLS.Api` (via `ACLS.Persistence`, EF Core), `ACLS.Worker` (via `ACLS.Persistence`) |
| **Connection** | Connection string from Azure Key Vault → environment variable `ACLS_DB_CONNECTION` |
| **Local dev** | SQL Server 2022 via Docker (`mcr.microsoft.com/mssql/server:2022-latest`) |

Schema is managed by EF Core code-first migrations in `ACLS.Persistence/Migrations/`. Full schema defined in `docs/04_Data/data_model_overview.md`.

#### Container 4: Azure Blob Storage

| Attribute | Value |
|---|---|
| **Technology** | Azure Blob Storage |
| **Accessed by** | `ACLS.Infrastructure.Storage.AzureBlobStorageService` (implements `IStorageService`) |
| **Purpose** | Store all media files uploaded by Residents (evidence) and Staff (completion photos) |
| **Local dev** | Azurite emulator via Docker |

Binary files are uploaded here. Only the resulting URL string is stored in the `Media` table in MSSQL. No binary content enters the relational database.

---

### 2.2 External Services (Provider-Agnostic)

These services are external to the ACLS system boundary. The interfaces `INotificationService` (email + SMS) are defined in `ACLS.Domain` and implemented in `ACLS.Infrastructure`. The specific providers below are the V1 choices — they can be swapped by changing the infrastructure implementation and configuration without touching any Domain or Application layer code.

| Service | Purpose | V1 Provider | Interface |
|---|---|---|---|
| Email | Transactional email (complaint updates, outage alerts, invitations) | Configurable (SendGrid / SMTP) | `INotificationService` |
| SMS | SMS notifications (outage alerts, SOS notifications) | Configurable (Twilio / Azure Communication Services) | `INotificationService` |
| Blob Storage | Media file storage | Azure Blob Storage | `IStorageService` |

---

## 3. Client Architecture

Five client applications. All are thin presentation layers — zero business logic.

| Client | Platform | Target Users | Tech | API Communication |
|---|---|---|---|---|
| `ResidentApp.Android` | Android 8+ (API 26+) | Residents | Kotlin + Jetpack Compose | Retrofit + OkHttp over HTTPS |
| `ResidentApp.iOS` | iOS 16+ | Residents | Swift + SwiftUI | URLSession over HTTPS |
| `StaffApp.Android` | Android 8+ (API 26+) | Maintenance Staff | Kotlin + Jetpack Compose | Retrofit + OkHttp over HTTPS |
| `StaffApp.iOS` | iOS 16+ | Maintenance Staff | Swift + SwiftUI | URLSession over HTTPS |
| `ResidentApp.Web` | Modern evergreen browsers | Property Managers | React + Next.js + TypeScript | Axios over HTTPS |

All clients:
- Authenticate by calling `POST /api/v1/auth/login` and storing the returned JWT
- Attach the JWT as a `Bearer` token on every subsequent request
- Never pass `propertyId` in request bodies (it is in the JWT)
- Render data exactly as returned by the API — no client-side sorting, filtering, or computation
- Handle API errors by reading the `errorCode` field from the Problem Details response

---

## 4. Request Lifecycle

### 4.1 Standard Request (e.g. Assign Complaint)

```
Client
  │
  │  POST /api/v1/complaints/42/assign
  │  Authorization: Bearer <jwt>
  │  Body: { "staffMemberId": 5 }
  ▼
TenancyMiddleware
  │  Reads property_id claim from JWT
  │  Populates ICurrentPropertyContext (scoped)
  ▼
[Authorize(Roles = "Manager")]
  │  Validates role claim
  ▼
ComplaintsController.AssignComplaint()
  │  Maps request → AssignComplaintCommand
  │  Sends via IMediator.Send()
  ▼
ValidationBehaviour (MediatR pipeline)
  │  Runs AssignComplaintValidator
  │  Returns 400 if invalid
  ▼
TransactionBehaviour (MediatR pipeline)
  │  Opens DB transaction
  ▼
AssignComplaintCommandHandler
  │  Loads Complaint (filtered by PropertyId) from IComplaintRepository
  │  Loads StaffMember (filtered by PropertyId) from IStaffRepository
  │  Calls complaint.Assign(staffMemberId)   ← raises ComplaintAssignedEvent
  │  Calls staff.MarkBusy()
  │  Calls IComplaintRepository.UpdateAsync()
  │  Calls IStaffRepository.UpdateAsync()
  │  Publishes ComplaintAssignedEvent
  ▼
TransactionBehaviour
  │  Commits transaction (both writes atomic)
  ▼
ComplaintsController
  │  Maps Result<ComplaintDto> → HTTP 200 OK
  ▼
Client receives ComplaintDto
  │
  (async, non-blocking)
  ▼
ComplaintAssignedEventHandler (ACLS.Worker)
  │  Calls INotificationService.NotifyStaff()
  ▼
Staff receives push/SMS notification
```

### 4.2 Media Upload Request (e.g. Submit Complaint with Photos)

```
Client
  │  POST /api/v1/complaints
  │  Content-Type: multipart/form-data
  │  Fields: title, description, category, urgency, permissionToEnter
  │  Files: photo1.jpg, photo2.jpg
  ▼
ComplaintsController.SubmitComplaint()
  │
  │  For each file in request.MediaFiles:
  │    url = await IStorageService.UploadAsync(fileStream, fileName)
  │                                ↑
  │                  AzureBlobStorageService uploads to Azure Blob
  │                  Returns permanent blob URL string
  │
  │  Maps request + urls → SubmitComplaintCommand
  ▼
SubmitComplaintCommandHandler
  │  Creates Complaint entity
  │  Creates Media entity per URL (url string only — no binary)
  │  Persists Complaint + Media to MSSQL
  ▼
Client receives ComplaintDto with media[].url populated
```

### 4.3 SOS Emergency Request

```
Client (Resident)
  │  POST /api/v1/complaints/sos
  ▼
TriggerSosCommandHandler
  │  Creates Complaint (urgency=SOS_EMERGENCY, status=OPEN)
  │  Persists to DB
  │  Publishes SosTriggeredEvent
  ▼
TransactionBehaviour commits
  │
  (async)
  ▼
SosTriggeredEventHandler (ACLS.Worker)
  │  Queries all StaffMembers WHERE availability=AVAILABLE AND propertyId=X
  │  Calls INotificationService.NotifyAllOnCallStaff(staffList, complaint)
  │    → Dispatches SMS + push to ALL available staff simultaneously
  │  Updates complaint.Status = ASSIGNED (system assignment)
  ▼
All on-call staff receive simultaneous emergency alert
```

### 4.4 Outage Declaration and Broadcast

```
Manager
  │  POST /api/v1/outages
  ▼
DeclareOutageCommandHandler
  │  Creates Outage entity
  │  Persists to DB
  │  Publishes OutageDeclaredEvent
  ▼
TransactionBehaviour commits
  │
  (async — non-blocking, must not delay HTTP response)
  ▼
BroadcastOutageNotificationJob (ACLS.Worker)
  │  Queries all Residents WHERE propertyId = outage.propertyId
  │  For each resident: dispatches Email + SMS concurrently
  │  Target: 500 messages within 60 seconds (NFR-12)
  │  On completion: updates Outage.NotificationSentAt = UtcNow
  ▼
Manager HTTP response: 201 Created (OutageDto, notificationSentAt = null)
  (notificationSentAt populates asynchronously after Worker completes)
```

---

## 5. Multi-Tenancy Architecture

Multi-tenancy in ACLS is row-level isolation. All properties share a single database and a single API deployment. Isolation is enforced at the query layer by mandatory `PropertyId` filtering.

```
┌─────────────────────────────────────────────────────────────┐
│                    Single ACLS API Instance                 │
│                                                             │
│  Request A: Manager at Property 1                          │
│  JWT claim: property_id = "1"                              │
│  TenancyMiddleware → ICurrentPropertyContext.PropertyId = 1 │
│  All repository queries: WHERE PropertyId = 1              │
│                                                             │
│  Request B: Manager at Property 2                          │
│  JWT claim: property_id = "2"                              │
│  TenancyMiddleware → ICurrentPropertyContext.PropertyId = 2 │
│  All repository queries: WHERE PropertyId = 2              │
│                                                             │
│  ← These requests share nothing. No data crosses boundary. │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │   Single SQL Database  │
              │                        │
              │  Properties table      │
              │   PropertyId=1: Sunset │
              │   PropertyId=2: Maple  │
              │                        │
              │  Complaints table      │
              │   PropertyId=1: rows   │
              │   PropertyId=2: rows   │
              └────────────────────────┘
```

**How it is enforced:**

1. `TenancyMiddleware` reads `property_id` from the validated JWT after `UseAuthentication()`.
2. It populates the scoped `ICurrentPropertyContext` service with `PropertyId` and `UserId`.
3. Every repository method that queries a property-scoped entity accepts `int propertyId` as a parameter and applies `.Where(x => x.PropertyId == propertyId)`.
4. `ICurrentPropertyContext.PropertyId` is the only permitted source of this value in command and query handlers.
5. Controllers never accept `propertyId` from clients.

A repository query that omits the `PropertyId` filter is a multi-tenancy breach. See `docs/07_Implementation/patterns/multi_tenancy_pattern.md` for implementation detail.

---

## 6. Security Model

### 6.1 Authentication

- JWT Bearer tokens issued by `ACLS.Api` on successful login
- Tokens signed with HMAC-SHA256 using the key at `ACLS_JWT_SECRET` environment variable
- Token lifetime: 60 minutes
- No refresh tokens in V1 — clients re-authenticate on expiry

### 6.2 Authorisation

Role-based access control enforced by `[Authorize(Roles = "...")]` on controller actions.

| Role | Can access |
|---|---|
| `Resident` | Own complaints only. Own feedback. Outages for their property. |
| `Manager` | All data scoped to their `PropertyId`. All complaint management. All reports. |
| `MaintenanceStaff` | Only their assigned complaints. Own availability. Their own profile. |

A `Resident` requesting another resident's complaint receives `404 Not Found` (not 403) — the resource does not exist within their scope. This prevents enumeration of other residents' complaint IDs.

### 6.3 Transport Security

- All production traffic is HTTPS (TLS 1.2+)
- HTTP → HTTPS redirect enforced by Azure App Service and `app.UseHttpsRedirection()` in the middleware pipeline
- Local development uses HTTP for simplicity (port 5000)

### 6.4 Password Security

- Passwords hashed using BCrypt with a minimum work factor of 12
- Plaintext passwords never logged, stored, or transmitted after the initial `POST /auth/login`
- Password hashes stored in `Users.PasswordHash` column — never returned in any API response

### 6.5 Secret Management

| Secret | Storage |
|---|---|
| Database connection string | Azure Key Vault → `ACLS_DB_CONNECTION` env var |
| JWT signing secret | Azure Key Vault → `ACLS_JWT_SECRET` env var |
| Blob storage connection | Azure Key Vault → `ACLS_STORAGE_CONNECTION` env var |
| Notification provider API key | Azure Key Vault → `ACLS_NOTIFICATION_KEY` env var |
| Local dev secrets | `appsettings.Development.json` (gitignored) |

No secret may be committed to source control. See `docs/07_Implementation/coding_standards.md` Section 6.

---

## 7. Deployment Architecture

### 7.1 Environments

| Environment | Purpose | Database | URL |
|---|---|---|---|
| `dev` (local) | Developer workstation | Docker SQL Server | `http://localhost:5000` |
| `staging` | Pre-production integration testing | Azure SQL (Standard tier) | `https://acls-api-staging.azurewebsites.net` |
| `prod` | Live system | Azure SQL (Standard/Premium tier) | `https://acls-api.azurewebsites.net` |

### 7.2 Azure Resources (per environment)

| Resource | Service | Notes |
|---|---|---|
| `acls-api-<env>` | Azure App Service | Hosts `ACLS.Api` |
| `acls-worker-<env>` | Azure App Service / Container App | Hosts `ACLS.Worker` |
| `acls-sql-<env>` | Azure SQL Database | Primary relational store |
| `acls-storage-<env>` | Azure Blob Storage Account | Media file storage |
| `acls-keyvault-<env>` | Azure Key Vault | All secrets |

All Azure resources are defined as Terraform HCL in `infrastructure/terraform/`. No manual resource creation in the portal.

### 7.3 CI/CD Pipeline

```
Developer pushes to feature branch
  ▼
GitHub Actions: backend-ci.yml
  │  dotnet restore
  │  dotnet build ACLS.sln
  │  dotnet test (unit tests)
  │  dotnet test (integration tests via TestContainers)
  ▼
Pull Request merged to main
  ▼
GitHub Actions: backend-ci.yml (on main)
  │  All tests pass
  │  dotnet publish
  │  Deploy to staging (Azure App Service)
  ▼
Manual approval gate
  ▼
Deploy to production
```

Migrations are applied automatically on startup in staging and production via:
```csharp
// In Program.cs (non-development environments)
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();
await db.Database.MigrateAsync();
```

---

## 8. Observability

### 8.1 Logging

Structured logging via `Microsoft.Extensions.Logging` with OpenTelemetry log bridge. Log levels:

| Level | When |
|---|---|
| `Information` | Request received, command handled, domain event published |
| `Warning` | Validation failure, unexpected null from repository, slow query |
| `Error` | Unhandled exception, infrastructure failure, transaction rollback |
| `Critical` | Database unreachable, Key Vault unreachable |

Never log: passwords, JWT tokens, `PasswordHash` values, full request bodies containing personal data.

### 8.2 Health Checks

`GET /healthz` — returns 200 if API is running and database is reachable.

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddAzureBlobStorage(storageConnectionString, name: "storage");

app.MapHealthChecks("/healthz");
```

### 8.3 Performance Targets

| Metric | Target | NFR |
|---|---|---|
| Core page load (GET endpoints) | ≤ 2 seconds | NFR-02 |
| Outage broadcast | 500 messages within 60 seconds | NFR-12 |
| System availability | ≥ 99% | NFR-01 |
| Supported units | 5,000 without degradation | NFR-03 |

---

## 9. Local Development Setup

Full local stack runs via Docker Compose. A developer with Docker Desktop and the .NET 8 SDK can run the complete system with one command:

```bash
./tools/dev-environment/bootstrap.sh
```

This script:
1. Starts Docker Compose services (SQL Server, Azurite blob emulator, MailHog email capture)
2. Waits for SQL Server to be ready
3. Applies EF Core migrations (`dotnet ef database update`)
4. Runs the seed script (`tools/scripts/seed-db.ps1`)
5. Starts `ACLS.Api` on `http://localhost:5000`

Docker Compose services:

| Service | Image | Port | Purpose |
|---|---|---|---|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | Primary database |
| `azurite` | `mcr.microsoft.com/azure-storage/azurite` | 10000 | Blob storage emulator |
| `mailhog` | `mailhog/mailhog` | 1025 (SMTP), 8025 (UI) | Email capture for notification testing |

OpenAPI/Swagger UI available at `http://localhost:5000/swagger` in development.

---

## 10. Architectural Decisions (ADR Index)

The following ADRs document key architectural decisions. Full text is in `docs/00_Project_Constitution/architectural_decisions/`.

| ADR | Decision |
|---|---|
| `0001_clean_architecture.md` | Clean Architecture (Onion) as the backend structural pattern |
| `0002_mssql_efcore.md` | MSSQL + EF Core code-first as the persistence stack |
| `0003_media_storage_abstraction.md` | `IStorageService` abstraction — binary files never in SQL |
| `0004_notification_abstraction.md` | `INotificationService` abstraction — provider chosen via configuration |
| `0005_row_level_multitenancy.md` | Row-level multi-tenancy with `PropertyId` discriminator (not separate schemas/databases) |
| `0006_jwt_self_issued.md` | Self-issued JWT tokens (no external identity provider in V1) |
| `0007_integer_primary_keys.md` | Integer PKs over GUIDs for join performance at target scale |
| `0008_enum_strings_in_db.md` | Enums stored as strings in MSSQL for readability and migration safety |

---

## 11. What This Architecture Does Not Include (V1 Scope)

The following are explicitly out of scope for V1. They are documented here so Antigravity Claude does not generate code for them:

- **Push notifications (FCM/APNs):** V1 uses SMS and email only. Push notification infrastructure is a V2 concern.
- **WebSockets / SignalR for real-time dashboard:** The manager dashboard polls the REST API. Real-time push via SignalR is V2.
- **Separate read database / CQRS projections:** Reporting queries run against the same MSSQL database. A read replica is a V2 scaling concern.
- **Rate limiting:** Not implemented in V1.
- **Refresh tokens:** Clients re-authenticate on JWT expiry. Refresh tokens are V2.
- **Multi-language / i18n:** English only in V1.
- **In-app messaging between residents and managers:** Not in scope. Communication happens via complaint status updates and notifications.
- **Resident web portal:** Residents use mobile apps only in V1. A browser-based resident portal is V2.
- **Service marketplace / third-party contractor booking:** Out of scope entirely.

If a session prompt requests any of the above, flag it as out of V1 scope before proceeding.

---

*End of System Overview v1.0*
