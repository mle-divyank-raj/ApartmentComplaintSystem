# Data Dictionary

**Document:** `docs/04_Data/data_dictionary.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This document defines every field in the ACLS database schema with its business meaning. For technical types, lengths, and constraints, see `docs/04_Data/data_model_overview.md`. For relationships, see `docs/04_Data/schema/erd.md`. This document explains the *business meaning* of each field.

---

## Properties

| Field | Business meaning |
|---|---|
| `PropertyId` | Unique identifier for an apartment complex. The root of all data isolation. |
| `Name` | Display name of the apartment complex (e.g. "Sunset Apartments") |
| `Address` | Full street address for display and identification |
| `IsActive` | Whether this property is currently managed on the platform. Inactive properties cannot receive new complaints. |

## Buildings

| Field | Business meaning |
|---|---|
| `BuildingId` | Unique identifier for a physical building within a property |
| `Name` | Display name (e.g. "Block A", "Tower 1") |

## Units

| Field | Business meaning |
|---|---|
| `UnitId` | Unique identifier for an apartment |
| `UnitNumber` | Apartment identifier within its building (e.g. "4B", "101") |
| `Floor` | Floor number. 0 = ground floor, negative values for basement levels |

## Users

| Field | Business meaning |
|---|---|
| `UserId` | Unique user identifier across the entire system |
| `Email` | Login credential and notification delivery address |
| `PasswordHash` | BCrypt-hashed password. The plaintext password is never stored. |
| `Phone` | Optional mobile number in E.164 format. Used for SMS notifications. |
| `FirstName` / `LastName` | Display name shown in the Manager Dashboard and notification messages |
| `Role` | The user's function in the system: `Resident`, `Manager`, or `MaintenanceStaff` |
| `PropertyId` | The property this user belongs to. Defines their data scope. |
| `IsActive` | Whether the user can log in. Set to false by a Manager to deactivate access. |
| `LastLoginAt` | Timestamp of the most recent successful login. Used by Manager for account activity monitoring. |

## Residents

| Field | Business meaning |
|---|---|
| `UnitId` | The apartment this resident occupies. A resident is linked to exactly one unit. |
| `LeaseStart` / `LeaseEnd` | Lease dates. Optional — for Manager reference only. Not used in business logic. |

## StaffMembers

| Field | Business meaning |
|---|---|
| `JobTitle` | The staff member's trade or role (e.g. "Plumber", "Electrician"). Shown to Manager during assignment. |
| `Skills` | A JSON list of skills (e.g. `["Plumbing","General"]`). Matched against complaint `RequiredSkills` by the Smart Dispatch algorithm. |
| `Availability` | Current work state: `AVAILABLE` (can accept work), `BUSY` (has active assignment), `ON_BREAK`, `OFF_DUTY`. Set manually except `BUSY` which is set by the system. |
| `AverageRating` | Mean of all `ResidentRating` values for complaints resolved by this staff member. Computed asynchronously by `ACLS.Worker`. |
| `LastAssignedAt` | Timestamp of the most recent complaint assignment. Used by Smart Dispatch to calculate `IdleScore`. |

## Complaints

| Field | Business meaning |
|---|---|
| `ComplaintId` | Unique ticket identifier shown to residents (e.g. "#42") |
| `PropertyId` | Property this complaint belongs to. Mandatory filter on all queries. |
| `UnitId` | The apartment the complaint is about |
| `ResidentId` | The resident who submitted the complaint |
| `AssignedStaffMemberId` | The staff member currently assigned. Null when OPEN. |
| `Title` | One-line summary of the issue |
| `Description` | Full description of the problem |
| `Category` | Type of maintenance required (e.g. "Plumbing", "Electrical") |
| `RequiredSkills` | JSON list of skills needed to resolve — derived from Category at submission, used by dispatch algorithm |
| `Urgency` | Severity level: `LOW`, `MEDIUM`, `HIGH`, `SOS_EMERGENCY` |
| `Status` | Current lifecycle state — see TicketStatus state machine |
| `PermissionToEnter` | Whether the resident consents to staff entering their unit when absent |
| `Eta` | Staff-set estimated completion time. Null until staff sets it. |
| `CreatedAt` | When the complaint was submitted |
| `UpdatedAt` | When any field on the complaint was last changed |
| `ResolvedAt` | When the complaint was marked RESOLVED by staff |
| `Tat` | Turn-around time in minutes (ResolvedAt − CreatedAt). Computed asynchronously by Worker. |
| `ResidentRating` | 1–5 star rating submitted by resident after resolution. Null until feedback given. |
| `ResidentFeedbackComment` | Optional written feedback from resident. Visible to Manager and resolving Staff only. |
| `FeedbackSubmittedAt` | When the resident submitted feedback |

## Media

| Field | Business meaning |
|---|---|
| `Url` | Full URL to the file in Azure Blob Storage. This is the only media content stored in MSSQL — never binary data. |
| `Type` | MIME type of the file (e.g. `"image/jpeg"`) |
| `UploadedByUserId` | Who uploaded this file — either a Resident (evidence) or Staff (completion photo) |
| `UploadedAt` | When the file was uploaded |

## WorkNotes

| Field | Business meaning |
|---|---|
| `Content` | Freetext note written by staff describing what they observed or did. Visible to the resident on the complaint detail screen. |
| `StaffMemberId` | Which staff member wrote the note |
| `CreatedAt` | When the note was added |

## InvitationTokens

| Field | Business meaning |
|---|---|
| `Token` | Cryptographically random string (64 hex chars). Sent to prospective resident via email link. |
| `UnitId` | The unit the invited resident will be linked to upon registration |
| `IssuedByManagerUserId` | Which manager generated this invitation |
| `ExpiresAt` | 72 hours after issuance. Token cannot be used after this time. |
| `UsedAt` | When the resident registered using this token. Null if unused. |
| `UsedByUserId` | Which resident used this token. Null if unused. |
| `IsRevoked` | Manager can revoke an invitation before it is used |

## Outages

| Field | Business meaning |
|---|---|
| `Title` | Short display name (e.g. "Planned Electricity Outage") |
| `OutageType` | Category of service disruption |
| `Description` | Full details including impact and any instructions for residents |
| `StartTime` / `EndTime` | The window of the outage |
| `DeclaredAt` | When the manager created the record |
| `NotificationSentAt` | When `ACLS.Worker` completed the resident broadcast. Null while notifications are in progress. |

## AuditLog

| Field | Business meaning |
|---|---|
| `Action` | What happened (e.g. `ComplaintAssigned`, `SosTriggered`) |
| `EntityType` | Which entity type was affected (e.g. `"Complaint"`) |
| `EntityId` | The primary key of the affected entity |
| `ActorUserId` | Who performed the action. Null for system-initiated events. |
| `ActorRole` | Role of the actor at the time of the action |
| `OldValue` | JSON snapshot of entity state before the change. Null for creation events. |
| `NewValue` | JSON snapshot of entity state after the change |
| `IpAddress` | Request origin IP. Null for background job events. |

---

*End of Data Dictionary v1.0*
