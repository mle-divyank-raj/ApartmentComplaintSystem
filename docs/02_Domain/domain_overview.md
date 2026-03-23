# Domain Overview

**Document:** `docs/02_Domain/domain_overview.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

## 1. Domain Summary

ACLS models the lifecycle of an apartment maintenance complaint from submission to closure. The domain has six bounded contexts, each owning a distinct slice of the business.

---

## 2. Bounded Contexts

| Context | Owns | Does not own |
|---|---|---|
| **Identity** | User registration, authentication, invitation tokens, roles | Complaint details, staff profiles |
| **Properties** | Property/Building/Unit hierarchy, PropertyId (tenancy root) | Users, complaints |
| **Complaints** | Complaint aggregate, Media, WorkNotes, TicketStatus state machine | Staff availability, dispatch logic |
| **Dispatch** | Smart Dispatch algorithm, StaffScore ranking | Assignment execution (hands back to Complaints context) |
| **Notifications** | INotificationService contract, delivery channels | Complaint content, staff data |
| **Outages** | Outage declaration, OutageType, broadcast trigger | Resident contact details (uses INotificationService) |
| **Reporting** | Read-side aggregations: TAT, average ratings, dashboard metrics | Write operations |
| **AuditLog** | Immutable event record for all critical actions | Business logic |

---

## 3. Core Aggregate

`Complaint` is the central aggregate. It owns the lifecycle from `OPEN` to `CLOSED`. All other contexts either produce or consume `Complaint` state via domain events.

```
Complaint lifecycle:
OPEN → ASSIGNED → EN_ROUTE → IN_PROGRESS → RESOLVED → CLOSED

Side effects per transition:
OPEN → ASSIGNED:      Staff.Availability = BUSY (atomic)
IN_PROGRESS → RESOLVED: Staff.Availability = AVAILABLE (atomic)
                        → ComplaintResolvedEvent → Worker → notify resident, calculate TAT
RESOLVED → CLOSED:    ResidentFeedback captured → FeedbackSubmittedEvent → Worker → update AverageRating
```

---

## 4. Cross-Context Communication

Contexts communicate exclusively via domain events — never via direct repository injection across boundaries.

| Event | Publisher | Consumers |
|---|---|---|
| `ComplaintSubmittedEvent` | Complaints | AuditLog |
| `ComplaintAssignedEvent` | Complaints | Notifications (notify staff), AuditLog |
| `ComplaintResolvedEvent` | Complaints | Notifications (notify resident), Reporting (TAT), AuditLog |
| `FeedbackSubmittedEvent` | Complaints | Reporting (AverageRating), AuditLog |
| `SosTriggeredEvent` | Complaints | Notifications (blast all staff), AuditLog |
| `OutageDeclaredEvent` | Outages | Notifications (broadcast to residents), AuditLog |
| `StaffAvailabilityChangedEvent` | Staff | AuditLog |

---

## 5. Multi-Tenancy in the Domain

Every entity in every context that is scoped to a property carries `PropertyId`. This is enforced structurally — not through runtime checks after the fact. See `docs/07_Implementation/patterns/multi_tenancy_pattern.md` for implementation.

---

*End of Domain Overview v1.0*
