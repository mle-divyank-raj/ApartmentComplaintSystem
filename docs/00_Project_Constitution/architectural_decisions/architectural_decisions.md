# ADR 0001 — Clean Architecture as Backend Structure

**File:** `docs/00_Project_Constitution/architectural_decisions/0001_clean_architecture.md`  
**Status:** Accepted  
**Date:** 2026-01-30

## Context
The ACLS backend needs a layering approach that keeps business logic independent of infrastructure choices (database, storage provider, notification provider) and supports long-term testability and maintainability.

## Decision
Adopt Clean Architecture (Onion Architecture) with four primary layers: SharedKernel, Domain, Application, and Infrastructure/Persistence. The dependency rule is strictly unidirectional — inner layers have zero knowledge of outer layers.

## Consequences
- Business logic in `ACLS.Domain` is completely independent of EF Core, ASP.NET Core, and all external SDKs
- All external dependencies are expressed as interfaces in Domain and implemented in Infrastructure
- Command/query handlers in Application can be unit tested with zero infrastructure setup
- Adding a new database or swapping Azure Blob for S3 requires only Infrastructure changes — zero Domain or Application changes
- Slightly more boilerplate (interfaces + implementations) compared to a flat architecture

## Alternatives Considered
- **Layered (N-tier):** Simpler but tightly couples business logic to data access
- **Vertical slices only:** Good for speed, poor for cross-cutting concerns like multi-tenancy and audit

---

# ADR 0002 — MSSQL with EF Core Code-First

**File:** `docs/00_Project_Constitution/architectural_decisions/0002_mssql_efcore.md`  
**Status:** Accepted  
**Date:** 2026-01-30

## Context
The system needs a relational database with strong support for transactions (required for atomic assignment/resolution), foreign key integrity, and indexing at the scale of 5,000 units.

## Decision
Use Microsoft SQL Server (Azure SQL in production, Docker SQL Server in development) as the primary relational store. Use Entity Framework Core with code-first migrations for schema management.

## Consequences
- Migrations are version-controlled in `ACLS.Persistence/Migrations/`
- Schema changes are tracked as code changes in pull requests
- EF Core's `SaveChangesAsync` participates in `TransactionBehaviour` for atomic multi-entity writes
- LINQ queries are strongly typed and compile-checked
- Azure SQL in production provides automatic backups, geo-redundancy, and point-in-time restore

## Alternatives Considered
- **PostgreSQL with Dapper:** More control but loses compile-time query checking and migrations tooling
- **MongoDB:** Unsuitable — the domain requires strong relational integrity (foreign keys, transactions across Complaint + StaffMember)

---

# ADR 0003 — IStorageService Abstraction for Media

**File:** `docs/00_Project_Constitution/architectural_decisions/0003_media_storage_abstraction.md`  
**Status:** Accepted  
**Date:** 2026-01-30

## Context
Complaint and resolution photos must be stored somewhere. Storing binary files in MSSQL causes unbounded row growth, slow queries, and backup size explosion. The storage provider may change (Azure Blob → S3) without requiring core changes.

## Decision
Define `IStorageService` in `ACLS.Domain` with a single method: `UploadAsync(stream, fileName, contentType) → string url`. Implement it in `ACLS.Infrastructure` as `AzureBlobStorageService` for staging/production and point to Azurite for local development. The `Media` table in MSSQL stores only the resulting URL string.

## Consequences
- Binary files are never in MSSQL — `Media.Url` is always a plain string
- Swapping from Azure Blob to S3 requires only replacing `AzureBlobStorageService` — zero Domain or Application changes
- Media URLs are permanent (not pre-signed with expiry) in V1
- Local development uses Azurite (Docker) — no Azure account required during development

---

# ADR 0004 — INotificationService Abstraction

**File:** `docs/00_Project_Constitution/architectural_decisions/0004_notification_abstraction.md`  
**Status:** Accepted  
**Date:** 2026-01-30

## Context
The system needs to send email and SMS notifications for multiple events (complaint assignment, resolution, outage broadcast, SOS blast). The specific provider (Twilio, SendGrid, Azure Communication Services) should be swappable without core changes.

## Decision
Define `INotificationService` in `ACLS.Domain` with four methods: `NotifyResident`, `NotifyStaff`, `NotifyAllOnCallStaff`, `BroadcastOutage`. Implement in `ACLS.Infrastructure`. Provider choice is a configuration concern — set via `appsettings.json` and environment variables. V1 provider decision deferred to infrastructure phase.

## Consequences
- Domain and Application layers have zero dependency on any specific notification provider
- Switching providers requires only Infrastructure changes
- `BroadcastOutage` and `NotifyAllOnCallStaff` must use concurrent dispatch (`Task.WhenAll`) in the implementation — NFR-12 compliance cannot be achieved with sequential calls
- Local development testing uses MailHog (Docker) to capture outgoing emails without a real provider

---
