# Data Model Overview

**Document:** `docs/04_Data/data_model_overview.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document is the single source of truth for the ACLS database schema. Every EF Core entity, every migration, every repository query, and every DTO must be consistent with the field definitions, types, constraints, and relationships described here. If a field is not in this document, it does not exist in the database. If a relationship is not described here, it does not exist as a foreign key.

---

## 1. Schema Principles

**All tables use integer primary keys** named `<EntityName>Id` (e.g. `ComplaintId`, `UnitId`). No GUIDs as primary keys — integer PKs perform better for joins in a relational schema of this size.

**All timestamps are UTC.** Every `DateTime` column stores UTC. `DateTime.UtcNow` is used exclusively. Columns are named with the `At` suffix for point-in-time values (`CreatedAt`, `ResolvedAt`) and with `Time` suffix for event window boundaries (`StartTime`, `EndTime`).

**Soft deletes are not used.** Deactivated users have `IsActive = false`. Closed complaints have `Status = CLOSED`. There are no `DeletedAt` columns or `IsDeleted` flags.

**All string columns have explicit max lengths.** No `nvarchar(MAX)` except where justified and documented. Justified exceptions: `Description` on `Complaints` (2000 chars), `ResidentFeedbackComment` on `Complaints` (1000 chars), `WorkNoteContent` on `WorkNotes` (2000 chars), `OldValue`/`NewValue` on `AuditLog` (4000 chars for JSON snapshots).

**Enum values are stored as strings** (`nvarchar(50)`), not integers. This makes the database human-readable and prevents silent bugs when enum values are reordered in code.

**Multi-tenancy column:** Every table that is scoped to a `Property` carries a `PropertyId` column with a non-nullable foreign key to `Properties`. Tables that are cross-property by design (`Properties`, `Buildings`, `Units`, `AuditLog`) are documented as such.

**Database:** MSSQL (SQL Server). EF Core with code-first migrations. All migrations live in `ACLS.Persistence/Migrations/`.

---

## 2. Entity Hierarchy Diagram (Textual)

```
Properties
└── Buildings (FK: PropertyId)
    └── Units (FK: BuildingId)
        └── Residents (FK: UnitId, UserId)

Users (base — all roles)
├── Residents (FK: UserId)           — extends Users, linked to Unit
└── StaffMembers (FK: UserId)        — extends Users, linked to Property

Complaints (FK: UnitId, ResidentId, AssignedStaffMemberId?, PropertyId)
├── Media (FK: ComplaintId)
└── WorkNotes (FK: ComplaintId, StaffMemberId)

InvitationTokens (FK: UnitId, IssuedByManagerId)
Outages (FK: PropertyId)
AuditLog (FK: PropertyId — nullable for cross-property system actions)
```

---

## 3. Tables and Fields

---

### 3.1 `Properties`

The root entity. One row per apartment complex managed on the platform.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `PropertyId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `Name` | `nvarchar(200)` | NO | NOT NULL | Display name of the property |
| `Address` | `nvarchar(500)` | NO | NOT NULL | Full street address |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |
| `IsActive` | `bit` | NO | NOT NULL, DEFAULT 1 | Soft-disable a property without deletion |

**Indexes:** PK on `PropertyId`.

**Relationships:** Parent of `Buildings`, `Outages`, `Complaints` (via `PropertyId`).

**Multi-tenancy note:** `Properties` is the tenancy root. It has no `PropertyId` FK to itself.

---

### 3.2 `Buildings`

A physical structure within a `Property`.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `BuildingId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `PropertyId` | `int` | NO | NOT NULL, FK → Properties | Multi-tenancy anchor |
| `Name` | `nvarchar(200)` | NO | NOT NULL | Building name or identifier (e.g. "Block A") |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |

**Indexes:** PK on `BuildingId`. IX on `PropertyId`.

**Relationships:** Belongs to `Properties`. Parent of `Units`.

---

### 3.3 `Units`

An individual apartment within a `Building`.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `UnitId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `BuildingId` | `int` | NO | NOT NULL, FK → Buildings | Parent building |
| `UnitNumber` | `nvarchar(20)` | NO | NOT NULL | e.g. "4B", "101", "PH2" |
| `Floor` | `int` | NO | NOT NULL | Floor number (0 = ground floor) |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |

**Indexes:** PK on `UnitId`. IX on `BuildingId`. Unique constraint on (`BuildingId`, `UnitNumber`).

**Relationships:** Belongs to `Buildings`. Parent of `Residents`, `Complaints` (via Unit), `InvitationTokens`.

---

### 3.4 `Users`

The base user table. All system actors (Residents, Managers, Maintenance Staff) have a row in `Users`.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `UserId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `Email` | `nvarchar(254)` | NO | NOT NULL, UNIQUE | Max per RFC 5321 |
| `PasswordHash` | `nvarchar(500)` | NO | NOT NULL | BCrypt hash — never plaintext |
| `Phone` | `nvarchar(20)` | YES | NULL allowed | E.164 format preferred (e.g. +12125551234) |
| `FirstName` | `nvarchar(100)` | NO | NOT NULL | |
| `LastName` | `nvarchar(100)` | NO | NOT NULL | |
| `Role` | `nvarchar(50)` | NO | NOT NULL | Enum string: `Resident`, `Manager`, `MaintenanceStaff` |
| `PropertyId` | `int` | NO | NOT NULL, FK → Properties | All users are scoped to one Property |
| `IsActive` | `bit` | NO | NOT NULL, DEFAULT 1 | Deactivated by Manager — cannot log in |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |
| `LastLoginAt` | `datetime2` | YES | NULL until first login | UTC timestamp |

**Indexes:** PK on `UserId`. UNIQUE on `Email`. IX on `PropertyId`. IX on `Role`.

**Valid `Role` values:** `Resident`, `Manager`, `MaintenanceStaff` — enforced by CHECK constraint and EF Core value conversion.

**Security note:** `PasswordHash` uses BCrypt with a work factor of 12 minimum. Plain passwords are never stored or logged.

**Relationships:** Extended by `Residents` and `StaffMembers` via one-to-one FK.

---

### 3.5 `Residents`

Extends `Users` with Resident-specific fields. One row per Resident user.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `ResidentId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `UserId` | `int` | NO | NOT NULL, FK → Users, UNIQUE | One-to-one with Users |
| `UnitId` | `int` | NO | NOT NULL, FK → Units | The Unit this Resident occupies |
| `LeaseStart` | `date` | YES | NULL if not tracked | Date portion only (no time) |
| `LeaseEnd` | `date` | YES | NULL if open-ended | Date portion only (no time) |

**Indexes:** PK on `ResidentId`. UNIQUE on `UserId`. IX on `UnitId`.

**Relationships:** One-to-one with `Users`. Belongs to `Units`. Parent of `Complaints` (as submitting resident).

**Note:** To get a Resident's `PropertyId`, join through `Users.PropertyId`. `Residents` does not duplicate the `PropertyId` column.

---

### 3.6 `StaffMembers`

Extends `Users` with Maintenance Staff-specific fields. One row per Staff user.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `StaffMemberId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `UserId` | `int` | NO | NOT NULL, FK → Users, UNIQUE | One-to-one with Users |
| `JobTitle` | `nvarchar(100)` | YES | NULL allowed | e.g. "Plumber", "Electrician", "General Maintenance" |
| `Skills` | `nvarchar(500)` | NO | NOT NULL, DEFAULT '[]' | JSON array of skill strings e.g. `["Plumbing","Electrical"]` |
| `Availability` | `nvarchar(50)` | NO | NOT NULL, DEFAULT 'AVAILABLE' | Enum string: `AVAILABLE`, `BUSY`, `ON_BREAK`, `OFF_DUTY` |
| `AverageRating` | `decimal(3,2)` | YES | NULL until first rating, CHECK (0.00-5.00) | Computed by Worker, not inline |
| `LastAssignedAt` | `datetime2` | YES | NULL until first assignment | UTC — used for IdleScore in dispatch |

**Indexes:** PK on `StaffMemberId`. UNIQUE on `UserId`. IX on `Availability` (filter index on `'AVAILABLE'` for dispatch queries). IX on `UserId`.

**Valid `Availability` values:** `AVAILABLE`, `BUSY`, `ON_BREAK`, `OFF_DUTY`.

**`Skills` field:** Stored as a JSON string in MSSQL (e.g. `'["Plumbing","Electrical","HVAC"]'`). EF Core maps this via a value converter (`JsonStringValueConverter<List<string>>`). The dispatch service deserialises this for skill intersection calculation. Skills are free-form strings — no separate skills lookup table in V1.

**`AverageRating`:** Updated asynchronously by `ACLS.Worker` after each `ResidentFeedback` submission. Never updated inline in a request handler.

**Relationships:** One-to-one with `Users`. Receives `Complaints` via `Complaints.AssignedStaffMemberId`.

---

### 3.7 `Complaints`

The central entity. One row per maintenance complaint submitted by a Resident.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `ComplaintId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `PropertyId` | `int` | NO | NOT NULL, FK → Properties | Multi-tenancy anchor — mandatory on every query |
| `UnitId` | `int` | NO | NOT NULL, FK → Units | The Unit the complaint is about |
| `ResidentId` | `int` | NO | NOT NULL, FK → Residents | The Resident who submitted the complaint |
| `AssignedStaffMemberId` | `int` | YES | NULL until assigned, FK → StaffMembers | Set atomically with Staff.Availability = BUSY |
| `Title` | `nvarchar(200)` | NO | NOT NULL | Short summary |
| `Description` | `nvarchar(2000)` | NO | NOT NULL | Full description of the issue |
| `Category` | `nvarchar(100)` | NO | NOT NULL | e.g. "Plumbing", "Electrical", "HVAC", "Structural", "Pest", "Other" |
| `RequiredSkills` | `nvarchar(500)` | NO | NOT NULL, DEFAULT '[]' | JSON array — skills needed to resolve; used by dispatch algorithm |
| `Urgency` | `nvarchar(50)` | NO | NOT NULL | Enum string: `LOW`, `MEDIUM`, `HIGH`, `SOS_EMERGENCY` |
| `Status` | `nvarchar(50)` | NO | NOT NULL, DEFAULT 'OPEN' | Enum string: `OPEN`, `ASSIGNED`, `EN_ROUTE`, `IN_PROGRESS`, `RESOLVED`, `CLOSED` |
| `PermissionToEnter` | `bit` | NO | NOT NULL, DEFAULT 0 | True = Staff may enter unit if Resident is absent |
| `Eta` | `datetime2` | YES | NULL until Staff sets it | UTC — estimated completion time set by Staff |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC — complaint submission time |
| `UpdatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC — last modification time; updated on every status change |
| `ResolvedAt` | `datetime2` | YES | NULL until resolved | UTC — set when Status transitions to RESOLVED |
| `Tat` | `decimal(10,2)` | YES | NULL until resolved | Minutes from CreatedAt to ResolvedAt; computed by Worker |
| `ResidentRating` | `int` | YES | NULL until feedback, CHECK (1-5) | Star rating 1–5 submitted by Resident |
| `ResidentFeedbackComment` | `nvarchar(1000)` | YES | NULL — comment is optional | Resident's written feedback |
| `FeedbackSubmittedAt` | `datetime2` | YES | NULL until CLOSED | UTC — when Resident submitted feedback |

**Indexes:**
- PK on `ComplaintId`
- IX on `PropertyId` (all queries filter by this)
- IX on (`PropertyId`, `Status`) — dashboard and triage queue queries
- IX on (`PropertyId`, `AssignedStaffMemberId`) — staff task list queries
- IX on (`PropertyId`, `UnitId`) — unit history queries
- IX on (`PropertyId`, `ResidentId`) — resident complaint history queries
- IX on `CreatedAt` — date range filtering

**Valid `Status` values and transitions:** Documented in `docs/02_Domain/ubiquitous_language.md` Section Part 3. Enforced in domain entity `Complaint.Assign()`, `Complaint.StartWork()`, `Complaint.Resolve()`, `Complaint.Close()` methods.

**Valid `Urgency` values:** `LOW`, `MEDIUM`, `HIGH`, `SOS_EMERGENCY`.

**`RequiredSkills`:** Parallel to `StaffMembers.Skills`. Stored as JSON string. Populated from `Category` by a mapping in `ComplaintService` at submission time (e.g. Category `"Plumbing"` → RequiredSkills `["Plumbing"]`). Can be overridden by Manager. Used by `IDispatchService` for `SkillScore` calculation.

**`Tat`:** Stored as decimal minutes for simple averaging. Calculated as `(ResolvedAt - CreatedAt).TotalMinutes` by `ACLS.Worker`. Nullable until resolved.

**Relationships:** Belongs to `Properties`, `Units`, `Residents`, optionally `StaffMembers`. Parent of `Media`, `WorkNotes`.

---

### 3.8 `Media`

One row per file attachment linked to a `Complaint`. Binary content is never stored here — only the blob storage URL.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `MediaId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `ComplaintId` | `int` | NO | NOT NULL, FK → Complaints | The complaint this media belongs to |
| `Url` | `nvarchar(2000)` | NO | NOT NULL | Full blob storage URL (Azure Blob or S3 pre-signed URL) |
| `Type` | `nvarchar(100)` | NO | NOT NULL | MIME type string e.g. `"image/jpeg"`, `"image/png"` |
| `UploadedByUserId` | `int` | NO | NOT NULL, FK → Users | Resident (evidence) or Staff (completion photo) |
| `UploadedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |

**Indexes:** PK on `MediaId`. IX on `ComplaintId`.

**Constraints:** Maximum 3 Media rows per `ComplaintId` where `UploadedByUserId` is a Resident. Enforced at application layer in `SubmitComplaintCommandHandler` — not as a database constraint.

**Binary files are never stored here.** The `Url` column holds a string URL only. The binary content lives in Azure Blob Storage / AWS S3. See `docs/07_Implementation/patterns/media_upload_pattern.md`.

---

### 3.9 `WorkNotes`

Freetext notes added by a `StaffMember` to an active `Complaint`.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `WorkNoteId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `ComplaintId` | `int` | NO | NOT NULL, FK → Complaints | The complaint this note is attached to |
| `StaffMemberId` | `int` | NO | NOT NULL, FK → StaffMembers | The staff member who wrote the note |
| `Content` | `nvarchar(2000)` | NO | NOT NULL | The note text |
| `CreatedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |

**Indexes:** PK on `WorkNoteId`. IX on `ComplaintId`.

**Relationships:** Belongs to `Complaints` and `StaffMembers`.

---

### 3.10 `InvitationTokens`

Secure single-use tokens generated by a Manager to onboard new Residents.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `InvitationTokenId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `Token` | `nvarchar(500)` | NO | NOT NULL, UNIQUE | Cryptographically random token string (e.g. 64-char hex) |
| `UnitId` | `int` | NO | NOT NULL, FK → Units | The Unit this invitation is for |
| `PropertyId` | `int` | NO | NOT NULL, FK → Properties | Denormalised for fast lookup without join |
| `IssuedByManagerUserId` | `int` | NO | NOT NULL, FK → Users | The Manager who generated this token |
| `IssuedAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |
| `ExpiresAt` | `datetime2` | NO | NOT NULL | UTC — `IssuedAt + 72 hours` |
| `UsedAt` | `datetime2` | YES | NULL until redeemed | UTC — set when Resident registers |
| `UsedByUserId` | `int` | YES | NULL until redeemed, FK → Users | The Resident who used this token |
| `IsRevoked` | `bit` | NO | NOT NULL, DEFAULT 0 | Manager can revoke unused tokens |

**Indexes:** PK on `InvitationTokenId`. UNIQUE on `Token`. IX on `UnitId`. IX on `PropertyId`.

**Expiry rule:** A token is valid only when `UsedAt IS NULL AND IsRevoked = 0 AND ExpiresAt > GETUTCDATE()`. Validated at application layer in `RegisterResidentCommandHandler`.

---

### 3.11 `Outages`

Property-wide service disruptions declared by a Manager.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `OutageId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `PropertyId` | `int` | NO | NOT NULL, FK → Properties | Multi-tenancy anchor |
| `DeclaredByManagerUserId` | `int` | NO | NOT NULL, FK → Users | The Manager who declared the outage |
| `Title` | `nvarchar(200)` | NO | NOT NULL | Short title e.g. "Planned Electricity Outage" |
| `OutageType` | `nvarchar(50)` | NO | NOT NULL | Enum string: `Electricity`, `Water`, `Gas`, `Internet`, `Elevator`, `Other` |
| `Description` | `nvarchar(2000)` | NO | NOT NULL | Full description and impact details |
| `StartTime` | `datetime2` | NO | NOT NULL | UTC — when the outage begins |
| `EndTime` | `datetime2` | NO | NOT NULL | UTC — when the outage is expected to end |
| `DeclaredAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC — when the record was created |
| `NotificationSentAt` | `datetime2` | YES | NULL until notifications dispatched | UTC — set by Worker after broadcast completes |

**Indexes:** PK on `OutageId`. IX on `PropertyId`. IX on (`PropertyId`, `StartTime`).

**Valid `OutageType` values:** `Electricity`, `Water`, `Gas`, `Internet`, `Elevator`, `Other`.

**`NotificationSentAt`:** Set by `ACLS.Worker` after the broadcast notification job completes. Used to verify NFR-12 compliance (500 messages within 60 seconds).

---

### 3.12 `AuditLog`

Immutable record of every significant action taken in the system. Rows are never updated or deleted.

| Column | Type | Nullable | Constraints | Notes |
|---|---|---|---|---|
| `AuditEntryId` | `int` | NO | PK, IDENTITY | Auto-increment primary key |
| `Action` | `nvarchar(100)` | NO | NOT NULL | Enum string — see AuditAction values below |
| `EntityType` | `nvarchar(100)` | NO | NOT NULL | e.g. `"Complaint"`, `"StaffMember"`, `"User"` |
| `EntityId` | `int` | NO | NOT NULL | PK of the affected entity |
| `ActorUserId` | `int` | YES | NULL for system-initiated actions | FK → Users (nullable for system events) |
| `ActorRole` | `nvarchar(50)` | YES | NULL for system-initiated actions | Role of the actor at time of action |
| `PropertyId` | `int` | YES | NULL for cross-property system actions | FK → Properties |
| `OccurredAt` | `datetime2` | NO | NOT NULL, DEFAULT GETUTCDATE() | UTC timestamp |
| `OldValue` | `nvarchar(4000)` | YES | NULL if no prior state | JSON snapshot of entity state before change |
| `NewValue` | `nvarchar(4000)` | YES | NULL for creation events | JSON snapshot of entity state after change |
| `IpAddress` | `nvarchar(45)` | YES | NULL for background jobs | IPv4 or IPv6 of the request origin |

**Indexes:** PK on `AuditEntryId`. IX on (`PropertyId`, `OccurredAt`). IX on (`EntityType`, `EntityId`). IX on `ActorUserId`.

**Valid `Action` values:** `ComplaintCreated`, `ComplaintAssigned`, `ComplaintReassigned`, `ComplaintStatusChanged`, `ComplaintResolved`, `ComplaintClosed`, `SosTriggered`, `OutageDeclared`, `StaffAvailabilityChanged`, `UserInvited`, `UserDeactivated`, `UserReactivated`, `MediaUploaded`, `FeedbackSubmitted`.

**Immutability:** EF Core entity configuration must not include `Update` or `Delete` operations on this table. The repository interface `IAuditRepository` exposes only `AddAsync` — no update or delete methods.

---

## 4. EF Core Entity Configuration Notes

### 4.1 Value Converters Required

These fields require EF Core value converters defined in `ACLS.Persistence/Configurations/`:

| Entity | Property | Stored as | Converter |
|---|---|---|---|
| `StaffMember` | `Skills` | `nvarchar(500)` JSON string | `JsonStringListConverter` |
| `Complaint` | `RequiredSkills` | `nvarchar(500)` JSON string | `JsonStringListConverter` |
| `Complaint` | `Status` | `nvarchar(50)` string | `TicketStatusConverter` |
| `Complaint` | `Urgency` | `nvarchar(50)` string | `UrgencyConverter` |
| `StaffMember` | `Availability` | `nvarchar(50)` string | `StaffStateConverter` |
| `User` | `Role` | `nvarchar(50)` string | `RoleConverter` |
| `Outage` | `OutageType` | `nvarchar(50)` string | `OutageTypeConverter` |

### 4.2 Table Name Conventions

EF Core is configured with explicit table names (not the EF Core pluralisation default):

| Entity class | Table name |
|---|---|
| `Property` | `Properties` |
| `Building` | `Buildings` |
| `Unit` | `Units` |
| `User` | `Users` |
| `Resident` | `Residents` |
| `StaffMember` | `StaffMembers` |
| `Complaint` | `Complaints` |
| `Media` | `Media` |
| `WorkNote` | `WorkNotes` |
| `InvitationToken` | `InvitationTokens` |
| `Outage` | `Outages` |
| `AuditEntry` | `AuditLog` |

### 4.3 Cascade Delete Rules

| Relationship | On Delete |
|---|---|
| `Properties` → `Buildings` | Restrict (no cascade — must deactivate property first) |
| `Buildings` → `Units` | Restrict |
| `Units` → `Residents` | Restrict |
| `Users` → `Residents` | Restrict |
| `Users` → `StaffMembers` | Restrict |
| `Complaints` → `Media` | Cascade (delete complaint → delete its media records) |
| `Complaints` → `WorkNotes` | Cascade |
| `Properties` → `Complaints` | Restrict |
| `Properties` → `Outages` | Restrict |
| `Units` → `InvitationTokens` | Restrict |

**No cascading deletes on user or property data.** Business deletion is handled by `IsActive = false` on `Users` and `Properties`.

### 4.4 Required EF Core Configuration per Entity

Each entity has a corresponding `IEntityTypeConfiguration<T>` class in `ACLS.Persistence/Configurations/`:

```
ACLS.Persistence/Configurations/
├── PropertyConfiguration.cs
├── BuildingConfiguration.cs
├── UnitConfiguration.cs
├── UserConfiguration.cs
├── ResidentConfiguration.cs
├── StaffMemberConfiguration.cs
├── ComplaintConfiguration.cs
├── MediaConfiguration.cs
├── WorkNoteConfiguration.cs
├── InvitationTokenConfiguration.cs
├── OutageConfiguration.cs
└── AuditEntryConfiguration.cs
```

Each configuration class applies:
1. Table name (explicit, from Section 4.2)
2. Primary key
3. All column names, types, max lengths, and nullability
4. Value converters (from Section 4.1)
5. Indexes and unique constraints
6. Foreign key relationships and cascade rules (from Section 4.3)
7. Default values where applicable

---

## 5. Derived / Computed Values

These values are stored in the database but computed asynchronously by `ACLS.Worker`. They are never calculated inline in a request handler.

| Field | Table | Computed from | Updated by |
|---|---|---|---|
| `Tat` | `Complaints` | `ResolvedAt - CreatedAt` (minutes) | Worker job triggered by `ComplaintResolvedEvent` |
| `AverageRating` | `StaffMembers` | Average of all `ResidentRating` for complaints resolved by this staff member | Worker job triggered by `FeedbackSubmittedEvent` |
| `NotificationSentAt` | `Outages` | System timestamp after broadcast completes | Worker job triggered by `OutageDeclaredEvent` |

---

## 6. Data Volumes and Sizing Assumptions

Based on NFR-03 (5,000 apartment units) and typical complaint rates:

| Entity | Expected rows (steady state) | Notes |
|---|---|---|
| `Properties` | 10–100 | Tens of properties per deployment |
| `Buildings` | 50–500 | ~5 buildings per property |
| `Units` | 5,000 | NFR-03 target |
| `Users` | 6,000–8,000 | ~1.2 residents + staff per unit |
| `Complaints` | 50,000–200,000 | ~10–40 complaints per unit per year |
| `Media` | 100,000–600,000 | ~3 media per complaint |
| `WorkNotes` | 50,000–200,000 | ~1 note per complaint |
| `AuditLog` | 500,000–2,000,000 | ~5–10 events per complaint lifecycle |
| `Outages` | 1,000–5,000 | Low frequency |
| `InvitationTokens` | 5,000–10,000 | ~1 per resident + reissuance |

All indexes defined in Section 3 are sized and placed for these volumes.

---

## 7. Seed Data Requirements

The following reference data must be seeded into a fresh development database by `tools/scripts/seed-db.ps1`:

**Properties:** 1 test property (`PropertyId = 1`, Name = "Sunset Apartments", Address = "123 Test St, Dallas TX 75201").

**Buildings:** 2 buildings in the test property (Block A, Block B).

**Units:** 10 units per building (20 total) — `UnitNumber` A101–A110, B101–B110.

**Users:**
- 1 Manager: `manager@test.acls`, password `Test@1234!`
- 2 Staff: `staff1@test.acls` (Skills: `["Plumbing","General"]`), `staff2@test.acls` (Skills: `["Electrical","HVAC"]`)
- 3 Residents: `resident1@test.acls` (Unit A101), `resident2@test.acls` (Unit A102), `resident3@test.acls` (Unit B101)

**Complaints:** 5 complaints across various statuses and urgency levels to exercise all dashboard views.

Seed script idempotent — running it twice does not duplicate data.

---

## 8. Migration Naming Convention

EF Core migrations follow this naming pattern:

```
YYYYMMDD_DescriptionOfChange

Examples:
20260201_InitialSchema
20260210_AddSkillsToStaffMember
20260215_AddNotificationSentAtToOutages
20260220_AddFeedbackFieldsToComplaints
```

The initial migration `20260201_InitialSchema` creates all tables defined in this document simultaneously. Subsequent migrations are incremental.

Migration commands:
```bash
dotnet ef migrations add 20260201_InitialSchema \
  --project ACLS.Persistence \
  --startup-project ACLS.Api

dotnet ef database update \
  --project ACLS.Persistence \
  --startup-project ACLS.Api
```

---

*End of Data Model Overview v1.0*
