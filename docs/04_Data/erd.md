# Entity Relationship Diagram

**Document:** `docs/04_Data/schema/erd.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This diagram is the authoritative visual representation of the ACLS database schema. It must remain in sync with `docs/04_Data/data_model_overview.md` at all times. If the two documents conflict, `data_model_overview.md` is the source of truth for field-level detail. This ERD is the source of truth for entity relationships and cardinality. When generating EF Core entity configurations or migrations, read both documents together.

---

## How to Read This Diagram

- `||--||` — exactly one to exactly one (one-to-one)
- `||--o{` — exactly one to zero or many (one-to-many, FK side is "many")
- `}o--||` — zero or many to exactly one
- `||--|{` — exactly one to one or many (one-to-many, mandatory children)

Relationship labels describe the action from left entity to right entity.

---

## Full Schema ERD

```mermaid
erDiagram

    Properties {
        int PropertyId PK
        nvarchar200 Name
        nvarchar500 Address
        datetime2 CreatedAt
        bit IsActive
    }

    Buildings {
        int BuildingId PK
        int PropertyId FK
        nvarchar200 Name
        datetime2 CreatedAt
    }

    Units {
        int UnitId PK
        int BuildingId FK
        nvarchar20 UnitNumber
        int Floor
        datetime2 CreatedAt
    }

    Users {
        int UserId PK
        int PropertyId FK
        nvarchar254 Email
        nvarchar500 PasswordHash
        nvarchar20 Phone
        nvarchar100 FirstName
        nvarchar100 LastName
        nvarchar50 Role
        bit IsActive
        datetime2 CreatedAt
        datetime2 LastLoginAt
    }

    Residents {
        int ResidentId PK
        int UserId FK
        int UnitId FK
        date LeaseStart
        date LeaseEnd
    }

    StaffMembers {
        int StaffMemberId PK
        int UserId FK
        nvarchar100 JobTitle
        nvarchar500 Skills
        nvarchar50 Availability
        decimal AverageRating
        datetime2 LastAssignedAt
    }

    Complaints {
        int ComplaintId PK
        int PropertyId FK
        int UnitId FK
        int ResidentId FK
        int AssignedStaffMemberId FK
        nvarchar200 Title
        nvarchar2000 Description
        nvarchar100 Category
        nvarchar500 RequiredSkills
        nvarchar50 Urgency
        nvarchar50 Status
        bit PermissionToEnter
        datetime2 Eta
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 ResolvedAt
        decimal Tat
        int ResidentRating
        nvarchar1000 ResidentFeedbackComment
        datetime2 FeedbackSubmittedAt
    }

    Media {
        int MediaId PK
        int ComplaintId FK
        int UploadedByUserId FK
        nvarchar2000 Url
        nvarchar100 Type
        datetime2 UploadedAt
    }

    WorkNotes {
        int WorkNoteId PK
        int ComplaintId FK
        int StaffMemberId FK
        nvarchar2000 Content
        datetime2 CreatedAt
    }

    InvitationTokens {
        int InvitationTokenId PK
        int UnitId FK
        int PropertyId FK
        int IssuedByManagerUserId FK
        int UsedByUserId FK
        nvarchar500 Token
        datetime2 IssuedAt
        datetime2 ExpiresAt
        datetime2 UsedAt
        bit IsRevoked
    }

    Outages {
        int OutageId PK
        int PropertyId FK
        int DeclaredByManagerUserId FK
        nvarchar200 Title
        nvarchar50 OutageType
        nvarchar2000 Description
        datetime2 StartTime
        datetime2 EndTime
        datetime2 DeclaredAt
        datetime2 NotificationSentAt
    }

    AuditLog {
        int AuditEntryId PK
        int PropertyId FK
        int ActorUserId FK
        nvarchar100 Action
        nvarchar100 EntityType
        int EntityId
        nvarchar50 ActorRole
        datetime2 OccurredAt
        nvarchar4000 OldValue
        nvarchar4000 NewValue
        nvarchar45 IpAddress
    }

    %% ── Property Hierarchy ──────────────────────────────────────
    Properties ||--|{ Buildings : "contains"
    Buildings  ||--|{ Units     : "contains"

    %% ── User Scoping ────────────────────────────────────────────
    Properties ||--o{ Users : "scopes"

    %% ── User Extensions (one-to-one) ────────────────────────────
    Users ||--|| Residents    : "extended by"
    Users ||--|| StaffMembers : "extended by"

    %% ── Resident lives in Unit ──────────────────────────────────
    Units ||--o{ Residents : "occupied by"

    %% ── Complaint relationships ─────────────────────────────────
    Properties   ||--o{ Complaints : "owns"
    Units        ||--o{ Complaints : "subject of"
    Residents    ||--o{ Complaints : "submits"
    StaffMembers ||--o{ Complaints : "assigned to"

    %% ── Complaint children ──────────────────────────────────────
    Complaints ||--o{ Media     : "has"
    Complaints ||--o{ WorkNotes : "has"

    %% ── Media and WorkNote authorship ───────────────────────────
    Users        ||--o{ Media     : "uploads"
    StaffMembers ||--o{ WorkNotes : "writes"

    %% ── Invitation Tokens ───────────────────────────────────────
    Units        ||--o{ InvitationTokens : "target of"
    Properties   ||--o{ InvitationTokens : "scopes"
    Users        ||--o{ InvitationTokens : "issued by"

    %% ── Outages ─────────────────────────────────────────────────
    Properties ||--o{ Outages : "declares"
    Users      ||--o{ Outages : "declared by"

    %% ── Audit Log ───────────────────────────────────────────────
    Properties ||--o{ AuditLog : "scopes"
    Users      ||--o{ AuditLog : "actor in"
```

---

## Relationship Notes

### Users → Residents and Users → StaffMembers (One-to-One)

`Residents` and `StaffMembers` are extension tables of `Users`. A `User` with `Role = Resident` has exactly one corresponding row in `Residents`. A `User` with `Role = MaintenanceStaff` has exactly one corresponding row in `StaffMembers`. A `User` with `Role = Manager` has no extension table row.

In EF Core this is modelled as a one-to-one owned or referenced relationship:

```csharp
// In UserConfiguration.cs
builder.HasOne<Resident>()
       .WithOne()
       .HasForeignKey<Resident>(r => r.UserId)
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne<StaffMember>()
       .WithOne()
       .HasForeignKey<StaffMember>(s => s.UserId)
       .OnDelete(DeleteBehavior.Restrict);
```

### Complaints → StaffMembers (Nullable FK)

`Complaints.AssignedStaffMemberId` is nullable. A newly submitted complaint has `AssignedStaffMemberId = NULL` and `Status = OPEN`. The FK is populated atomically when a Manager assigns the complaint (and simultaneously sets `StaffMember.Availability = BUSY`).

```csharp
// In ComplaintConfiguration.cs
builder.HasOne<StaffMember>()
       .WithMany()
       .HasForeignKey(c => c.AssignedStaffMemberId)
       .IsRequired(false)
       .OnDelete(DeleteBehavior.Restrict);
```

### Media → Users (Upload Authorship)

`Media.UploadedByUserId` references `Users` directly — not `Residents` or `StaffMembers` — because both Residents (evidence photos at submission) and Staff (completion photos at resolution) upload media. The application layer distinguishes the upload context from the `User.Role` on the authenticated user.

### InvitationTokens → Users (Two FKs)

`InvitationTokens` has two FKs to `Users`:
- `IssuedByManagerUserId` — the Manager who created the token (NOT NULL)
- `UsedByUserId` — the Resident who redeemed it (NULL until used)

These are two distinct relationships and must be configured separately in EF Core with explicit foreign key names to avoid shadow property conflicts.

```csharp
// In InvitationTokenConfiguration.cs
builder.HasOne<User>()
       .WithMany()
       .HasForeignKey(t => t.IssuedByManagerUserId)
       .HasConstraintName("FK_InvitationTokens_IssuedByManager")
       .OnDelete(DeleteBehavior.Restrict);

builder.HasOne<User>()
       .WithMany()
       .HasForeignKey(t => t.UsedByUserId)
       .HasConstraintName("FK_InvitationTokens_UsedByResident")
       .IsRequired(false)
       .OnDelete(DeleteBehavior.Restrict);
```

### AuditLog → Properties and AuditLog → Users (Nullable FKs)

Both `AuditLog.PropertyId` and `AuditLog.ActorUserId` are nullable to accommodate system-initiated events (background jobs, automated notifications) that do not originate from a user request and may not be scoped to a single property.

### Outages → Users (Declared By)

`Outages.DeclaredByManagerUserId` references `Users` directly. The application layer enforces that only a user with `Role = Manager` can call the `DeclareOutage` endpoint. The FK constraint itself does not restrict by role.

---

## Sub-Diagrams by Bounded Context

These sub-diagrams isolate each bounded context for easier reading when working on a specific area.

---

### Property Hierarchy

```mermaid
erDiagram

    Properties {
        int PropertyId PK
        nvarchar200 Name
        nvarchar500 Address
        bit IsActive
    }

    Buildings {
        int BuildingId PK
        int PropertyId FK
        nvarchar200 Name
    }

    Units {
        int UnitId PK
        int BuildingId FK
        nvarchar20 UnitNumber
        int Floor
    }

    Properties ||--|{ Buildings : "contains"
    Buildings  ||--|{ Units     : "contains"
```

---

### Identity and Users

```mermaid
erDiagram

    Users {
        int UserId PK
        int PropertyId FK
        nvarchar254 Email
        nvarchar100 FirstName
        nvarchar100 LastName
        nvarchar50 Role
        bit IsActive
    }

    Residents {
        int ResidentId PK
        int UserId FK
        int UnitId FK
        date LeaseStart
        date LeaseEnd
    }

    StaffMembers {
        int StaffMemberId PK
        int UserId FK
        nvarchar100 JobTitle
        nvarchar500 Skills
        nvarchar50 Availability
        decimal AverageRating
        datetime2 LastAssignedAt
    }

    InvitationTokens {
        int InvitationTokenId PK
        int UnitId FK
        int PropertyId FK
        int IssuedByManagerUserId FK
        int UsedByUserId FK
        nvarchar500 Token
        datetime2 ExpiresAt
        bit IsRevoked
    }

    Users ||--|| Residents    : "extended by"
    Users ||--|| StaffMembers : "extended by"
    Users ||--o{ InvitationTokens : "issued by"
    Users ||--o{ InvitationTokens : "redeemed by"
```

---

### Complaints Core

```mermaid
erDiagram

    Complaints {
        int ComplaintId PK
        int PropertyId FK
        int UnitId FK
        int ResidentId FK
        int AssignedStaffMemberId FK
        nvarchar200 Title
        nvarchar100 Category
        nvarchar50 Urgency
        nvarchar50 Status
        bit PermissionToEnter
        datetime2 Eta
        datetime2 CreatedAt
        datetime2 ResolvedAt
        decimal Tat
        int ResidentRating
    }

    Media {
        int MediaId PK
        int ComplaintId FK
        int UploadedByUserId FK
        nvarchar2000 Url
        nvarchar100 Type
        datetime2 UploadedAt
    }

    WorkNotes {
        int WorkNoteId PK
        int ComplaintId FK
        int StaffMemberId FK
        nvarchar2000 Content
        datetime2 CreatedAt
    }

    Complaints ||--o{ Media     : "has"
    Complaints ||--o{ WorkNotes : "has"
```

---

### Outages and Audit

```mermaid
erDiagram

    Outages {
        int OutageId PK
        int PropertyId FK
        int DeclaredByManagerUserId FK
        nvarchar200 Title
        nvarchar50 OutageType
        datetime2 StartTime
        datetime2 EndTime
        datetime2 DeclaredAt
        datetime2 NotificationSentAt
    }

    AuditLog {
        int AuditEntryId PK
        int PropertyId FK
        int ActorUserId FK
        nvarchar100 Action
        nvarchar100 EntityType
        int EntityId
        datetime2 OccurredAt
        nvarchar4000 OldValue
        nvarchar4000 NewValue
    }
```

---

## Index Summary

Quick reference for all non-PK indexes defined across the schema. Use this when writing EF Core `HasIndex()` configurations.

| Table | Index columns | Type | Purpose |
|---|---|---|---|
| `Users` | `Email` | UNIQUE | Login lookup, uniqueness enforcement |
| `Users` | `PropertyId` | Standard | Tenant scoping |
| `Users` | `Role` | Standard | Role-based filtering |
| `Buildings` | `PropertyId` | Standard | Property hierarchy traversal |
| `Units` | `BuildingId` | Standard | Building children query |
| `Units` | (`BuildingId`, `UnitNumber`) | UNIQUE | Prevent duplicate unit numbers within a building |
| `Residents` | `UserId` | UNIQUE | One-to-one enforcement |
| `Residents` | `UnitId` | Standard | Unit occupancy lookup |
| `StaffMembers` | `UserId` | UNIQUE | One-to-one enforcement |
| `StaffMembers` | `Availability` | Filtered (`= 'AVAILABLE'`) | Dispatch query — available staff only |
| `Complaints` | `PropertyId` | Standard | Mandatory on every complaint query |
| `Complaints` | (`PropertyId`, `Status`) | Standard | Dashboard and triage queue |
| `Complaints` | (`PropertyId`, `AssignedStaffMemberId`) | Standard | Staff task list |
| `Complaints` | (`PropertyId`, `UnitId`) | Standard | Unit complaint history |
| `Complaints` | (`PropertyId`, `ResidentId`) | Standard | Resident complaint history |
| `Complaints` | `CreatedAt` | Standard | Date range filtering and sorting |
| `Media` | `ComplaintId` | Standard | Media by complaint |
| `WorkNotes` | `ComplaintId` | Standard | Notes by complaint |
| `InvitationTokens` | `Token` | UNIQUE | Token lookup at registration |
| `InvitationTokens` | `UnitId` | Standard | Tokens for a unit |
| `InvitationTokens` | `PropertyId` | Standard | Tenant scoping |
| `Outages` | `PropertyId` | Standard | Tenant scoping |
| `Outages` | (`PropertyId`, `StartTime`) | Standard | Active/upcoming outage queries |
| `AuditLog` | (`PropertyId`, `OccurredAt`) | Standard | Audit trail by property and time |
| `AuditLog` | (`EntityType`, `EntityId`) | Standard | Audit trail for a specific entity |
| `AuditLog` | `ActorUserId` | Standard | Actions by a specific user |

---

*End of Entity Relationship Diagram v1.0*
