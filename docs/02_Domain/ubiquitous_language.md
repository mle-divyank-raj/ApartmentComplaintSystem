# Ubiquitous Language

**Document:** `docs/02_Domain/ubiquitous_language.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document is the single source of truth for every domain term used in this project. Every class name, variable name, API field name, database column name, UI label, and test description must use the terms defined here — exactly as defined here. If a term is not in this document, it does not exist in the codebase. If you encounter a synonym or variant of a term in existing code, treat it as a bug to be corrected.

---

## How to Use This Document

When naming anything — a C# class, a Kotlin data class, a TypeScript type, a database column, an API field, a UI label — look up the concept here first. Use the **Canonical Term** exactly. Never substitute a synonym, abbreviation, or informal alternative unless this document explicitly lists it as an alias.

If a concept you need to name is not in this document, state that explicitly before proceeding. Do not invent a name.

---

## Part 1 — Core Actor Terms

These terms name the people who interact with the system. They are used in class names, route paths, JWT role claims, UI labels, and test descriptions.

---

### Resident

**Canonical term:** `Resident`  
**Role claim value:** `"Resident"`  
**Database table:** `Residents`  
**C# class:** `Resident`  
**Kotlin class:** `Resident`  
**Swift struct:** `Resident`

A person who occupies a `Unit` within a `Building` within a `Property` and has a registered account on the platform. A Resident is linked to exactly one `Unit`. A Resident may only view and manage their own `Complaints`. A Resident cannot view other Residents' data.

**Must NOT be called:** Tenant, Occupant, User, Renter, Lessee, Customer, Client.

> **Why this matters:** "Tenant" is a reserved term in SaaS architecture meaning a SaaS organisation (a paying customer of the platform). Using "Tenant" to mean a person who rents an apartment will cause serious confusion with multi-tenancy concepts. "Occupant" is similarly ambiguous. `Resident` is the only correct term for this actor.

---

### Property Manager

**Canonical term:** `PropertyManager` (in code) / `Manager` (in route paths and short references)  
**Role claim value:** `"Manager"`  
**Database table:** `Users` (with `Role = Manager`)  
**C# class:** `User` with `Role.Manager`  
**UI label:** `Property Manager`

A person responsible for administering a `Property`. A Property Manager can view all `Complaints` across their `Property`, assign `Complaints` to `Staff`, declare `Outages`, manage user accounts, and access all analytics and reports.

A Property Manager is scoped to exactly one `Property` (determined by `PropertyId` in their JWT). They cannot access data from another `Property`.

**Must NOT be called:** Admin, Administrator, Landlord, Owner, Supervisor, PropertyAdmin.

> **Why this matters:** "Admin" is ambiguous — it could mean a system administrator with cross-property access, which is a fundamentally different security scope. `Manager` (or `PropertyManager`) precisely conveys the property-scoped administrative role.

---

### Maintenance Staff / Staff Member

**Canonical term:** `StaffMember` (in code) / `Staff` (in short references, route paths, API fields)  
**Role claim value:** `"MaintenanceStaff"`  
**Database table:** `StaffMembers`  
**C# class:** `StaffMember`  
**Kotlin class:** `StaffMember`  
**Swift struct:** `StaffMember`  
**UI label:** `Maintenance Staff`

A worker who receives `Complaint` assignments and resolves maintenance issues. A Staff Member can view only their own assigned `Complaints`. A Staff Member manages their own `Availability` status and updates `Complaint` progress.

**Must NOT be called:** Technician, Worker, Employee, Handyman, Contractor, Operative, MaintenanceWorker.

---

## Part 2 — Property Hierarchy Terms

The property hierarchy is the structural backbone of the system. Every piece of data in the system is anchored to a position in this hierarchy.

---

### Property

**Canonical term:** `Property`  
**Database table:** `Properties`  
**C# class:** `Property`  
**Primary key field:** `PropertyId`

A physical apartment complex or managed residential building registered on the platform. A `Property` is the root of the data hierarchy and the unit of multi-tenancy. All data — `Residents`, `Staff`, `Complaints`, `Outages` — belongs to exactly one `Property`.

**Must NOT be called:** Complex, Building (a Building is a sub-entity of a Property — see below), Premises, Site, Community.

---

### Building

**Canonical term:** `Building`  
**Database table:** `Buildings`  
**C# class:** `Building`  
**Primary key field:** `BuildingId`

A physical structure within a `Property`. A `Property` contains one or more `Buildings`. A `Building` contains one or more `Units`. A `Building` is never the root of data ownership — `Property` is.

**Must NOT be called:** Block, Tower, Structure.

---

### Unit

**Canonical term:** `Unit`  
**Database table:** `Units`  
**C# class:** `Unit`  
**Primary key field:** `UnitId`  
**UI label:** `Unit` or `Apartment Unit`

An individual apartment within a `Building`. A `Unit` belongs to exactly one `Building`. A `Resident` is linked to exactly one `Unit`. A `Unit` is identified by a `UnitNumber` (e.g. `"4B"`) and a `Floor` number.

**Must NOT be called:** Apartment, Room, Suite, Flat, Dwelling.

---

### PropertyId

**Canonical term:** `PropertyId`  
**C# type:** `PropertyId` (strongly-typed value object wrapping `int`)  
**Database column:** `property_id` (on every property-scoped table)  
**JWT claim name:** `"property_id"`

The strongly-typed identifier of the `Property` to which a user or data record belongs. `PropertyId` is the multi-tenancy discriminator. It is extracted exclusively from the authenticated user's JWT claim by `TenancyMiddleware`. It is never accepted from request bodies, query strings, or route parameters.

This is the most security-critical field in the system. Every repository query on a property-scoped entity must filter by `PropertyId`.

**Must NOT be treated as:** a regular integer field, a request parameter, something the client supplies, optional.

---

## Part 3 — Complaint (Ticket) Terms

The `Complaint` is the central domain entity. Precision in complaint-related terminology is critical because multiple statuses, urgency levels, and lifecycle events all have specific meaning.

---

### Complaint

**Canonical term:** `Complaint`  
**Alternate accepted term:** `Ticket` (acceptable in UI labels and informal references only — never in class names or database tables)  
**Database table:** `Complaints`  
**C# class:** `Complaint`  
**Primary key field:** `ComplaintId` (also referred to as `TicketId` in the UML diagram — `ComplaintId` is the canonical code name)  
**UI label:** `Complaint` or `Maintenance Request`

A maintenance issue reported by a `Resident`. A `Complaint` has a `Category`, `Title`, `Description`, `Urgency`, `Status`, and an optional set of `Media` attachments. A `Complaint` progresses through a defined `TicketStatus` lifecycle.

**Must NOT be called:** Request (alone), Issue, Problem, Ticket (in code), Report, Case.

---

### TicketStatus

**Canonical term:** `TicketStatus`  
**C# enum:** `TicketStatus`  
**Database column:** `status` (string or int mapped from enum)

The lifecycle state of a `Complaint`. The complete and exhaustive set of valid values is:

| Value | Meaning |
|---|---|
| `OPEN` | Complaint has been submitted by a Resident. Not yet assigned to any Staff. |
| `ASSIGNED` | A Manager has assigned the Complaint to a Staff Member. Staff has not yet acknowledged. |
| `EN_ROUTE` | Staff Member has acknowledged the assignment and is travelling to the Unit. |
| `IN_PROGRESS` | Staff Member has begun work on the Complaint at the Unit. |
| `RESOLVED` | Staff Member has marked the Complaint as complete. Resident has not yet provided feedback. |
| `CLOSED` | Resident has submitted a rating and feedback. Complaint is archived. Final state. |

**Transition rules (state machine — never violated):**

```
OPEN → ASSIGNED (by Manager, via AssignComplaint)
ASSIGNED → EN_ROUTE (by Staff, via AcceptAssignment)
EN_ROUTE → IN_PROGRESS (by Staff, via StartWork)
IN_PROGRESS → RESOLVED (by Staff, via ResolveComplaint)
RESOLVED → CLOSED (by Resident, via SubmitFeedback)

Special: OPEN → ASSIGNED also occurs for SOS complaints (system-initiated)
Special: ASSIGNED → ASSIGNED is valid for reassignment (Manager reassigns to different Staff)
```

No other transitions are valid. A `CLOSED` complaint cannot be reopened. A `RESOLVED` complaint cannot go back to `IN_PROGRESS`.

**Must NOT be called:** Status alone (ambiguous), State, Stage, Phase, Step.

---

### Urgency

**Canonical term:** `Urgency`  
**C# enum:** `Urgency`  
**Database column:** `urgency`  
**UI label:** `Priority` (acceptable in UI only — `Urgency` in code)

The severity level of a `Complaint` as assessed by the `Resident` at submission time. The complete and exhaustive set of valid values is:

| Value | UI Label | Meaning |
|---|---|---|
| `LOW` | Low | Non-urgent issue. No immediate impact on habitability. |
| `MEDIUM` | Medium | Noticeable issue. Should be resolved within normal SLA. |
| `HIGH` | High | Significant issue affecting comfort or safety. Prioritise. |
| `SOS_EMERGENCY` | EMERGENCY | Immediate threat to life or property (fire, flood, gas leak). Bypasses standard queue. Notifies all on-call Staff simultaneously. |

`SOS_EMERGENCY` is distinct from `HIGH`. An `SOS_EMERGENCY` triggers the SOS protocol: all on-call `StaffMembers` are notified simultaneously, the `Complaint` is immediately set to `ASSIGNED`, and the Smart Dispatch ranking is bypassed.

**Must NOT be called:** Priority (in code), Severity, Level, Grade.

---

### Media

**Canonical term:** `Media`  
**Database table:** `Media`  
**C# class:** `Media`  
**Primary key field:** `MediaId`

A file attachment linked to a `Complaint`. A `Media` record stores only the blob storage URL (a string) and metadata. Binary file content is never stored in the database. A `Complaint` may have up to 3 `Media` attachments uploaded by the `Resident` at submission time, and additional `Media` attachments uploaded by `Staff` as completion evidence.

`Media` uploaded by a Resident is evidence of the problem. `Media` uploaded by Staff is evidence of the resolution.

**Fields:** `MediaId`, `Url` (blob storage URL string), `Type` (MIME type string, e.g. `"image/jpeg"`), `ComplaintId` (FK), `UploadedByUserId` (FK), `UploadedAt` (DateTime).

**Must NOT be called:** Attachment, File, Image, Photo, Picture, Document, Evidence.

> **Why this matters:** `Media` is the canonical term used in the database table, the C# class, the API response, and all three frontend clients. Using inconsistent synonyms across layers causes mapping errors.

---

### WorkNote

**Canonical term:** `WorkNote`  
**Database table:** `WorkNotes`  
**C# class:** `WorkNote`  
**Primary key field:** `WorkNoteId`

A freetext note added by a `StaffMember` to an active `Complaint`. A `WorkNote` records what the staff member observed or did. Multiple `WorkNotes` may be added to a single `Complaint`.

**Must NOT be called:** Comment, Note, Remark, Log, Entry, Update.

---

### ETA

**Canonical term:** `Eta` (in C# property names, following .NET naming) / `eta` (in JSON API fields and database columns)  
**Full form:** Estimated Time of Completion  
**C# type:** `DateTime?` (nullable — not set until Staff acknowledges and assesses)  
**UI label:** `ETA` or `Estimated completion time`

The date and time by which the assigned `StaffMember` estimates the `Complaint` will be resolved. Set by the `StaffMember` after acknowledging the assignment and assessing the job. Updated by the `StaffMember` as work progresses. When `Eta` is updated, the `Resident` is automatically notified.

`Eta` is never set by the system automatically. It is always set by the `StaffMember`.

**Must NOT be called:** DueDate, Deadline, CompletionTime, EstimatedTime, TargetDate.

---

### TAT (Turn-Around Time)

**Canonical term:** `Tat` (in C# property names) / `tat` (in JSON API fields) / `TAT` (in UI labels and documentation prose)  
**Full form:** Turn-Around Time  
**C# type:** `TimeSpan?` or `double?` (minutes or hours, as specified by the API contract)  
**Calculation:** `ResolvedAt - CreatedAt`

The elapsed duration between `Complaint` creation and `Complaint` resolution. Calculated asynchronously by `ACLS.Worker` after a `Complaint` is resolved. Never calculated inline in a synchronous API request. Used in Manager reports and Staff performance summaries.

`TAT` is a read-only derived value. It is never set directly. It is computed from `CreatedAt` and `ResolvedAt`.

**Must NOT be called:** ResolutionTime, SolveTime, Duration, TimeToResolve, TimeTaken.

---

## Part 4 — Staff State Terms

---

### StaffState / Availability

**Canonical term:** `StaffState` (in C# enum type name) / `Availability` (in property name and UI label)  
**C# enum:** `StaffState`  
**Database column:** `availability`  
**UI label:** `Availability`

The current work state of a `StaffMember`. The complete and exhaustive set of valid values is:

| Value | UI Label | Meaning |
|---|---|---|
| `AVAILABLE` | Available | Staff Member is on duty and can accept new assignments. |
| `BUSY` | Busy | Staff Member is currently assigned to an active Complaint. Cannot receive new assignments via Smart Dispatch. |
| `ON_BREAK` | On Break | Staff Member is temporarily unavailable. Not eligible for Smart Dispatch. |
| `OFF_DUTY` | Off Duty | Staff Member is not on shift. Not eligible for Smart Dispatch. |

`AVAILABLE` is the only state eligible for Smart Dispatch recommendations. When a `Complaint` is assigned, the `StaffMember` moves to `BUSY` atomically in the same transaction. When a `Complaint` is resolved, the `StaffMember` returns to `AVAILABLE` atomically in the same transaction.

**Must NOT be called:** Status (alone, ambiguous with TicketStatus), State (alone), WorkStatus, EmployeeStatus.

---

## Part 5 — Dispatch Terms

---

### Smart Dispatch

**Canonical term:** `SmartDispatch` (in documentation) / `DispatchService` (in code)  
**Interface:** `IDispatchService`  
**Implementation class:** `DispatchService`

The algorithmic system that ranks available `StaffMembers` for a given `Complaint` based on skill match and idle time. `SmartDispatch` returns a ranked list — it does not make the assignment. The `Manager` reviews the ranked list and makes the final assignment decision.

**Must NOT be called:** AutoAssign, AutoDispatch, Recommendation Engine, Matching Algorithm.

---

### StaffScore

**Canonical term:** `StaffScore`  
**C# class:** `StaffScore`  
**API response field:** `staffScores` (array)

The output unit of `IDispatchService`. A `StaffScore` pairs a `StaffMember` with a computed `MatchScore` (a `double` between 0.0 and the urgency-weighted ceiling). The `IDispatchService` returns `List<StaffScore>` ordered by `MatchScore` descending.

**Fields:** `StaffMember` (the candidate), `MatchScore` (double), `SkillScore` (double, component), `IdleScore` (double, component).

**Must NOT be called:** RankedStaff, StaffRanking, CandidateScore, MatchResult.

---

### MatchScore

**Canonical term:** `MatchScore`  
**C# property name:** `MatchScore`  
**API field:** `matchScore`  
**Type:** `double`

The computed suitability score for a `StaffMember` candidate for a given `Complaint`. Calculated as `(SkillScore × 0.6 + IdleScore × 0.4) × UrgencyWeight`. Higher is better. Used only for ranking — never displayed directly to the Manager as a raw number (the UI shows ranked order, not the score value).

---

### SkillScore

**Canonical term:** `SkillScore`  
**C# property:** `SkillScore`  
**Type:** `double` (0.0–1.0)

The proportion of a `Complaint`'s required skills that a `StaffMember` possesses. Calculated as `count(intersection(staff.Skills, complaint.RequiredSkills)) ÷ count(complaint.RequiredSkills)`. If a `Complaint` has no required skills, all candidates receive `SkillScore = 1.0`.

---

### IdleScore

**Canonical term:** `IdleScore`  
**C# property:** `IdleScore`  
**Type:** `double` (0.0–1.0)

A normalised measure of how long a `StaffMember` has been idle (i.e. without an active assignment). Calculated by normalising `(DateTime.UtcNow - staff.LastAssignedAt)` against the maximum idle time in the current candidate pool. A staff member who has been idle longest receives `IdleScore = 1.0`. A staff member who was just freed receives `IdleScore ≈ 0.0`.

---

## Part 6 — Outage Terms

---

### Outage

**Canonical term:** `Outage`  
**Database table:** `Outages`  
**C# class:** `Outage`  
**Primary key field:** `OutageId`  
**UI label:** `Outage` or `Property-Wide Outage`

A property-wide service disruption declared by a `Manager`. An `Outage` has a `Title`, `OutageType`, `StartTime`, `EndTime`, `Description`, and `PropertyId`. Declaring an `Outage` triggers a mass notification to all `Residents` of the `Property` via email and SMS.

**Must NOT be called:** Alert, Incident, Event, Disruption, Maintenance (alone).

---

### OutageType

**Canonical term:** `OutageType`  
**C# enum:** `OutageType`  
**Database column:** `outage_type`

The category of a declared `Outage`. Valid values: `Electricity`, `Water`, `Gas`, `Internet`, `Elevator`, `Other`.

---

### Broadcast

**Canonical term:** `Broadcast` (as a verb and concept) / `BroadcastOutage` (as a method name)

The act of sending a mass notification to all `Residents` of a `Property` simultaneously. A `Broadcast` is triggered by `Outage` declaration and by `SOS_EMERGENCY` complaints (to Staff). The `INotificationService.BroadcastOutage(outage)` method handles property-wide resident notification. The `INotificationService.NotifyAllOnCallStaff(staffList, complaint)` method handles SOS staff notification.

**Must NOT be called:** MassNotification, BlastNotification, BulkSend, FanOut.

---

## Part 7 — Notification Terms

---

### Notification

**Canonical term:** `Notification`  
**Interface:** `INotificationService`

An automated message sent to a `Resident` or `StaffMember` triggered by a system event. Notifications are delivered via `NotificationChannel`.

---

### NotificationChannel

**Canonical term:** `NotificationChannel`  
**C# enum:** `NotificationChannel`  
**Valid values:** `Email`, `SMS`, `InApp`, `Push`

The delivery mechanism for a `Notification`. The specific provider (e.g. Twilio, SendGrid) is an infrastructure concern determined by `appsettings.json` configuration. `NotificationChannel` is a domain concept — it does not reference any specific provider.

---

### InvitationToken

**Canonical term:** `InvitationToken`  
**C# class:** `InvitationToken`  
**Database table:** `InvitationTokens`  
**UI label:** `Invitation Link`

A secure, single-use token generated by a `Manager` and sent to a prospective `Resident` via email. The `Resident` uses the `InvitationToken` to register an account, which automatically links their account to the specified `Unit`. An `InvitationToken` expires after 72 hours if unused.

**Must NOT be called:** RegistrationToken, SignupLink, OnboardingToken, InviteCode.

---

## Part 8 — Audit Terms

---

### AuditEntry

**Canonical term:** `AuditEntry`  
**Database table:** `AuditLog`  
**C# class:** `AuditEntry`  
**Primary key field:** `AuditEntryId`

An immutable record of a significant action taken in the system. Written for every `Complaint` status change, every assignment, every `Outage` declaration, and every SOS trigger. `AuditEntries` are never modified or deleted. They form the immutable audit trail required by NFR-10.

**Fields:** `AuditEntryId`, `Action` (AuditAction enum), `EntityId`, `EntityType`, `ActorId` (UserId), `ActorRole`, `PropertyId`, `OccurredAt` (DateTime, UTC), `OldValue` (JSON string, nullable), `NewValue` (JSON string, nullable).

**Must NOT be called:** Log, LogEntry, ActivityLog, ChangeRecord, EventLog.

---

### AuditAction

**Canonical term:** `AuditAction`  
**C# enum:** `AuditAction`

The type of action recorded in an `AuditEntry`. Valid values:

`ComplaintCreated`, `ComplaintAssigned`, `ComplaintReassigned`, `ComplaintStatusChanged`, `ComplaintResolved`, `ComplaintClosed`, `SosTriggered`, `OutageDeclared`, `StaffAvailabilityChanged`, `UserInvited`, `UserDeactivated`, `UserReactivated`, `MediaUploaded`, `FeedbackSubmitted`.

---

## Part 9 — Reporting Terms

---

### StaffPerformanceSummary

**Canonical term:** `StaffPerformanceSummary`  
**C# class:** `StaffPerformanceSummary` (read model)  
**API response type:** `StaffPerformanceSummaryDto`

A read model aggregating performance data for a single `StaffMember`. Contains: `StaffMemberId`, `FullName`, `TotalResolved` (int), `AverageRating` (double, 1.0–5.0), `AverageTat` (TimeSpan or double in minutes).

Calculated asynchronously by `ACLS.Worker`. Never computed inline in an API request handler.

---

### ResidentFeedback

**Canonical term:** `ResidentFeedback` (conceptually) / individual fields `ResidentRating` and `ResidentFeedbackComment` (in database columns and C# properties)  
**Database columns:** `resident_rating` (int, 1–5, nullable), `resident_feedback_comment` (nvarchar, nullable)  
**UI label:** `Feedback` or `Rate your experience`

A rating (1–5 stars) and optional comment submitted by a `Resident` after a `Complaint` is `RESOLVED`. Submitting feedback moves the `Complaint` from `RESOLVED` to `CLOSED`. `ResidentFeedback` is visible only to the `Manager` and optionally to the `StaffMember` who resolved the `Complaint`. It is never visible to other `Residents`.

**Must NOT be called:** Review, Rating (alone), Score, Survey, Comment (alone).

---

## Part 10 — Architectural Terms

These terms describe the system's architecture and must be used consistently in technical discussions, commit messages, and code comments.

---

### Multi-Tenancy

**Canonical term:** `Multi-tenancy` (in documentation) / enforced via `PropertyId` (in code)

The architectural property that ensures data from one `Property` is never accessible to users of another `Property`. Multi-tenancy in ACLS is enforced at the repository layer via mandatory `PropertyId` filtering, injected by `TenancyMiddleware` from the authenticated JWT. It is not enforced by separate databases or schemas.

**The word "tenant" in the SaaS sense refers to a `Property` — not a `Resident`.** The terms must never be used interchangeably.

---

### TenancyMiddleware

**Canonical term:** `TenancyMiddleware`  
**C# class:** `TenancyMiddleware`  
**Location:** `ACLS.Api/Middleware/TenancyMiddleware.cs`

The ASP.NET Core middleware that intercepts every authenticated request, reads the `property_id` claim from the JWT, and populates `ICurrentPropertyContext` for the duration of the request. All downstream components (controllers, command handlers, repositories) read `PropertyId` exclusively from `ICurrentPropertyContext`.

---

### ICurrentPropertyContext

**Canonical term:** `ICurrentPropertyContext`  
**C# interface:** `ICurrentPropertyContext`  
**Registered lifetime:** Scoped (per HTTP request)

The scoped service that exposes the current request's `PropertyId` and `UserId` to any component that needs them. Injected via constructor injection. Never bypassed.

---

### Domain Event

**Canonical term:** `DomainEvent` (base class concept) / specific events named `<Noun><PastTense>Event`

An immutable record of something significant that happened in the domain. Published by aggregate roots after a state change is committed. Consumed by `INotificationService`, `ACLS.Worker`, and `IAuditRepository`. Domain events are the only permitted mechanism for cross-bounded-context communication.

Examples: `ComplaintAssignedEvent`, `ComplaintResolvedEvent`, `OutageDeclaredEvent`, `SosTriggeredEvent`.

---

## Part 11 — Terms That Must Never Appear in Code

The following terms are explicitly banned from class names, variable names, method names, API fields, database columns, and UI labels. If you encounter them in generated code, they must be renamed to the canonical equivalent.

| Banned term | Use instead |
|---|---|
| `Tenant` (meaning a person who rents) | `Resident` |
| `Occupant` | `Resident` |
| `Admin` (meaning Property Manager) | `Manager` or `PropertyManager` |
| `Technician` | `StaffMember` |
| `Handyman` | `StaffMember` |
| `Worker` | `StaffMember` |
| `Apartment` | `Unit` |
| `Flat` | `Unit` |
| `Complex` (meaning Property) | `Property` |
| `Issue` (meaning Complaint) | `Complaint` |
| `Ticket` (in code — acceptable in UI only) | `Complaint` |
| `Comment` (meaning WorkNote) | `WorkNote` |
| `Attachment` | `Media` |
| `Photo` | `Media` |
| `DueDate` | `Eta` |
| `ResolutionTime` | `Tat` |
| `Priority` (in code — acceptable in UI only) | `Urgency` |
| `Status` alone (ambiguous) | `TicketStatus` or `StaffState` depending on context |
| `State` alone (ambiguous) | `TicketStatus` or `StaffState` depending on context |
| `AutoAssign` | `SmartDispatch` or `DispatchService` |
| `Incident` (meaning Outage) | `Outage` |
| `BulkSend` | `Broadcast` |
| `Review` (meaning ResidentFeedback) | `ResidentFeedback` or `ResidentRating` |
| `Log` alone (meaning AuditEntry) | `AuditEntry` |

---

*End of Ubiquitous Language v1.0*
