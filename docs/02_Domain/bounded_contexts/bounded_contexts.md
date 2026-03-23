# Bounded Contexts

**Document:** `docs/02_Domain/bounded_contexts/` (combined reference)  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> Each section below corresponds to one file in `docs/02_Domain/bounded_contexts/`. Split into individual files when creating the repo: identity.md, complaints.md, dispatch.md, notifications.md, outages.md, reporting.md.

---

# identity.md

## Identity Bounded Context

**Owns:** `User`, `Resident`, `StaffMember`, `InvitationToken`, `Role`

**Responsibility:** Manages who can access the system and in what capacity. Handles registration, authentication, and the invitation-token onboarding workflow.

**Key rules:**
- A `User` with `Role = Resident` must have a corresponding `Resident` row (one-to-one)
- A `User` with `Role = MaintenanceStaff` must have a corresponding `StaffMember` row (one-to-one)
- A `User` with `Role = Manager` has no extension table
- `InvitationToken` expires 72 hours after issuance and is single-use
- `PropertyId` on `User` is the tenancy discriminator — all users are scoped to exactly one property
- Password hashing: BCrypt with minimum work factor 12
- JWT tokens are issued by `ACLS.Api`, signed with HMAC-SHA256, lifetime 60 minutes

**Domain events published:** `UserRegisteredEvent`

**Does not own:** Complaint details, staff performance metrics, availability state

---

# complaints.md

## Complaints Bounded Context

**Owns:** `Complaint`, `Media`, `WorkNote`, `TicketStatus`, `Urgency`, `ComplaintErrors`, `ComplaintConstants`

**Responsibility:** The core business context. Owns the complete lifecycle of a maintenance complaint from submission to closure.

**State machine (authoritative):**
```
OPEN → ASSIGNED (Manager assigns, or system assigns for SOS)
ASSIGNED → EN_ROUTE (Staff acknowledges)
EN_ROUTE → IN_PROGRESS (Staff begins work)
IN_PROGRESS → RESOLVED (Staff resolves)
RESOLVED → CLOSED (Resident submits feedback)

Reassignment: ASSIGNED → ASSIGNED (Manager reassigns to different staff)
```
No other transitions are valid. A `CLOSED` complaint is immutable.

**Key rules:**
- Maximum 3 `Media` attachments per complaint (combined resident + staff)
- Binary media content never stored in the database — only blob URLs
- `ASSIGNED` transition must atomically set `StaffMember.Availability = BUSY`
- `RESOLVED` transition must atomically set `StaffMember.Availability = AVAILABLE`
- `Tat` is computed asynchronously by `ACLS.Worker` — never inline
- `ResidentFeedback` visible only to Manager and optionally the resolving Staff (NFR-13)

**Domain events published:** `ComplaintSubmittedEvent`, `ComplaintAssignedEvent`, `ComplaintStatusChangedEvent`, `ComplaintResolvedEvent`

---

# dispatch.md

## Dispatch Bounded Context

**Owns:** `IDispatchService`, `StaffScore`, `DispatchCriteria`, `DispatchWeights`

**Responsibility:** The Smart Dispatch algorithm. Accepts a `Complaint` and returns a ranked list of available `StaffMembers` scored by skill match and idle time. Never makes assignments — only ranks.

**Algorithm (canonical formula):**
```
matchScore = (skillScore × 0.6 + idleScore × 0.4) × urgencyWeight
urgencyWeight = 2.0 for SOS_EMERGENCY, 1.0 for all others
```

Full specification: `docs/07_Implementation/patterns/dispatch_algorithm.md`

**Does not own:** The assignment operation itself (that belongs to Complaints context), staff persistence

---

# notifications.md

## Notifications Bounded Context

**Owns:** `INotificationService`, `NotificationChannel`

**Responsibility:** Defines the contract for delivering notifications to Residents and Staff. The specific provider (email, SMS) is a configuration concern implemented in `ACLS.Infrastructure`.

**Methods:**
- `NotifyResident(residentId, complaint)` — status change, ETA update, resolution
- `NotifyStaff(staffMemberId, complaint)` — new assignment
- `NotifyAllOnCallStaff(staffList, complaint)` — SOS blast (concurrent, not sequential)
- `BroadcastOutage(outage)` — mass notification to all property residents

**Key rule:** `NotifyAllOnCallStaff` and `BroadcastOutage` dispatch notifications concurrently using `Task.WhenAll()`. Sequential loops are forbidden (NFR-12).

**Does not own:** Complaint content, staff or resident contact details — receives these as parameters

---

# outages.md

## Outages Bounded Context

**Owns:** `Outage`, `OutageType`

**Responsibility:** Models property-wide service disruptions. An `Outage` triggers a broadcast notification to all residents via `INotificationService`.

**Key rules:**
- Only a `Manager` may declare an `Outage`
- `endTime` must be after `startTime`
- `NotificationSentAt` is set by `ACLS.Worker` after broadcast completes — not inline in the handler
- `OutageType` values: `Electricity`, `Water`, `Gas`, `Internet`, `Elevator`, `Other`

**Domain events published:** `OutageDeclaredEvent`

---

# reporting.md

## Reporting Bounded Context

**Owns:** `IReportingService`, `StaffPerformanceSummary`, `DashboardMetricsDto`

**Responsibility:** Read-side aggregation. Produces performance summaries and dashboard metrics. All calculations are either pre-computed by `ACLS.Worker` and stored in the database, or computed by optimised queries at read time.

**Key rules:**
- `AverageRating` on `StaffMember` is updated by `UpdateAverageRatingJob` in `ACLS.Worker`
- `Tat` on `Complaint` is computed by `CalculateTatJob` in `ACLS.Worker`
- No write operations — this context is read-only
- `ResidentFeedback` (rating + comment) never exposed to other residents (NFR-13)

**Does not own:** Write operations on any entity

---
