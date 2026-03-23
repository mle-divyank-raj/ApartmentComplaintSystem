# Error Codes

**Document:** `docs/05_API/error_codes.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document is the single source of truth for all `errorCode` values returned in API error responses. Every `errorCode` string used in `ACLS.Domain` error classes, `ACLS.Api` responses, the `packages/error-codes` TypeScript package, and all three frontend clients must match exactly the values defined here. No errorCode may be used in code that is not defined in this document.

---

## 1. Error Response Format

All errors follow RFC 7807 Problem Details. The `errorCode` field is the machine-readable identifier clients use for programmatic error handling — not the `title` or `detail` fields, which are human-readable and may change.

```json
{
  "type": "https://acls.api/errors/complaint-not-found",
  "title": "Complaint not found",
  "status": 404,
  "detail": "Complaint with ID 42 was not found.",
  "instance": "/api/v1/complaints/42",
  "errorCode": "Complaint.NotFound"
}
```

**Client error handling pattern:**
```typescript
// TypeScript (ResidentApp.Web)
if (error.errorCode === ErrorCodes.Complaint.NotFound) {
  showNotFoundMessage();
}

// Kotlin (Android)
when (error.errorCode) {
  "Complaint.NotFound" -> showNotFoundScreen()
  "Auth.InvalidCredentials" -> showLoginError()
}
```

---

## 2. Error Code Catalogue

### 2.1 Auth Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Auth.InvalidCredentials` | 401 | Email/password combination does not match |
| `Auth.TokenExpired` | 401 | JWT has expired — client must re-authenticate |
| `Auth.TokenInvalid` | 401 | JWT signature invalid or malformed |
| `Auth.MissingPropertyClaim` | 401 | JWT does not contain a valid `property_id` claim |
| `Auth.AccountDeactivated` | 401 | User account has been deactivated by a Manager |
| `Auth.InsufficientRole` | 403 | Authenticated user's role does not permit this operation |
| `Auth.InvitationTokenInvalid` | 400 | Invitation token does not exist |
| `Auth.InvitationTokenExpired` | 400 | Invitation token has passed its 72-hour expiry |
| `Auth.InvitationTokenAlreadyUsed` | 409 | Invitation token was already redeemed |
| `Auth.InvitationTokenRevoked` | 400 | Invitation token was revoked by the Manager |
| `Auth.EmailAlreadyRegistered` | 409 | An account with this email already exists |

---

### 2.2 Complaint Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Complaint.NotFound` | 404 | Complaint does not exist within the authenticated user's property scope |
| `Complaint.AccessDenied` | 404 | Complaint belongs to a different property (returns 404 not 403 — see multi_tenancy_pattern.md Section 7) |
| `Complaint.InvalidStatusTransition` | 422 | Requested status transition is not valid per the TicketStatus state machine |
| `Complaint.AlreadyClosed` | 422 | Complaint is CLOSED and cannot be modified |
| `Complaint.AlreadyResolved` | 422 | Complaint is already RESOLVED |
| `Complaint.NotAssigned` | 422 | Operation requires complaint to be ASSIGNED — it is not |
| `Complaint.MaxMediaAttachmentsExceeded` | 400 | Complaint already has 3 media attachments — the maximum |
| `Complaint.InvalidMediaType` | 400 | File type is not accepted — only JPEG and PNG are allowed |
| `Complaint.MediaFileTooLarge` | 400 | File exceeds the 5MB size limit |
| `Complaint.StaffNotAvailable` | 422 | Selected staff member is not AVAILABLE for assignment |
| `Complaint.StaffBelongsToDifferentProperty` | 404 | Staff member does not belong to this property |
| `Complaint.FeedbackAlreadySubmitted` | 409 | Resident has already submitted feedback for this complaint |
| `Complaint.FeedbackNotAllowed` | 422 | Feedback can only be submitted when status is RESOLVED |

---

### 2.3 Staff Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Staff.NotFound` | 404 | Staff member does not exist within the authenticated property scope |
| `Staff.CannotSetBusyManually` | 422 | `BUSY` availability cannot be set manually — only set by the system when a complaint is assigned |
| `Staff.NotAuthorizedToUpdateOtherStaff` | 403 | Staff member attempted to update another staff member's availability |

---

### 2.4 User Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `User.NotFound` | 404 | User does not exist within the authenticated property scope |
| `User.AlreadyDeactivated` | 409 | User account is already deactivated |
| `User.AlreadyActive` | 409 | User account is already active |
| `User.CannotDeactivateSelf` | 422 | Manager attempted to deactivate their own account |

---

### 2.5 Outage Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Outage.NotFound` | 404 | Outage does not exist within the authenticated property scope |
| `Outage.EndTimeBeforeStartTime` | 400 | `endTime` is before or equal to `startTime` |
| `Outage.StartTimeInPast` | 400 | `startTime` is in the past |

---

### 2.6 Dispatch Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Dispatch.NoStaffAvailable` | 200 | No staff are currently AVAILABLE — returns empty recommendations list with this code in the response body (not an HTTP error — HTTP 200 with empty array) |

---

### 2.7 Validation Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `Validation.Failed` | 400 | One or more request fields failed FluentValidation — accompanied by the `errors` map |

---

### 2.8 System Errors

| Error Code | HTTP Status | When thrown |
|---|---|---|
| `System.InternalError` | 500 | Unhandled infrastructure failure — details not exposed to client |
| `System.StorageUnavailable` | 503 | Azure Blob Storage is unreachable — media upload cannot proceed |
| `System.DatabaseUnavailable` | 503 | Database is unreachable |

---

## 3. C# Error Class Conventions

Each bounded context defines a static `Errors` class in `ACLS.Domain`. Error codes are defined as string constants — never inline strings.

```csharp
// ACLS.Domain/Complaints/ComplaintErrors.cs
public static class ComplaintErrors
{
    public static Error NotFound(int complaintId) =>
        new("Complaint.NotFound",
            $"Complaint with ID {complaintId} was not found.");

    public static Error InvalidStatusTransition(TicketStatus from, TicketStatus to) =>
        new("Complaint.InvalidStatusTransition",
            $"Cannot transition complaint from {from} to {to}.");

    public static Error MaxMediaAttachmentsExceeded() =>
        new("Complaint.MaxMediaAttachmentsExceeded",
            $"A complaint may not have more than {ComplaintConstants.MaxMediaAttachments} media attachments.");

    public static Error StaffNotAvailable(int staffMemberId) =>
        new("Complaint.StaffNotAvailable",
            $"Staff member {staffMemberId} is not AVAILABLE for assignment.");

    public static Error FeedbackNotAllowed(TicketStatus currentStatus) =>
        new("Complaint.FeedbackNotAllowed",
            $"Feedback can only be submitted when the complaint is RESOLVED. Current status: {currentStatus}.");
}
```

---

## 4. TypeScript Error Codes Package

**Location:** `packages/error-codes/src/index.ts`

This package mirrors the C# error classes and is consumed by `ResidentApp.Web`. It is the only permitted source of error code strings in the React frontend.

```typescript
// packages/error-codes/src/index.ts
export const ErrorCodes = {
  Auth: {
    InvalidCredentials: 'Auth.InvalidCredentials',
    TokenExpired: 'Auth.TokenExpired',
    TokenInvalid: 'Auth.TokenInvalid',
    MissingPropertyClaim: 'Auth.MissingPropertyClaim',
    AccountDeactivated: 'Auth.AccountDeactivated',
    InsufficientRole: 'Auth.InsufficientRole',
    InvitationTokenInvalid: 'Auth.InvitationTokenInvalid',
    InvitationTokenExpired: 'Auth.InvitationTokenExpired',
    InvitationTokenAlreadyUsed: 'Auth.InvitationTokenAlreadyUsed',
    InvitationTokenRevoked: 'Auth.InvitationTokenRevoked',
    EmailAlreadyRegistered: 'Auth.EmailAlreadyRegistered',
  },
  Complaint: {
    NotFound: 'Complaint.NotFound',
    AccessDenied: 'Complaint.AccessDenied',
    InvalidStatusTransition: 'Complaint.InvalidStatusTransition',
    AlreadyClosed: 'Complaint.AlreadyClosed',
    AlreadyResolved: 'Complaint.AlreadyResolved',
    NotAssigned: 'Complaint.NotAssigned',
    MaxMediaAttachmentsExceeded: 'Complaint.MaxMediaAttachmentsExceeded',
    InvalidMediaType: 'Complaint.InvalidMediaType',
    MediaFileTooLarge: 'Complaint.MediaFileTooLarge',
    StaffNotAvailable: 'Complaint.StaffNotAvailable',
    StaffBelongsToDifferentProperty: 'Complaint.StaffBelongsToDifferentProperty',
    FeedbackAlreadySubmitted: 'Complaint.FeedbackAlreadySubmitted',
    FeedbackNotAllowed: 'Complaint.FeedbackNotAllowed',
  },
  Staff: {
    NotFound: 'Staff.NotFound',
    CannotSetBusyManually: 'Staff.CannotSetBusyManually',
    NotAuthorizedToUpdateOtherStaff: 'Staff.NotAuthorizedToUpdateOtherStaff',
  },
  User: {
    NotFound: 'User.NotFound',
    AlreadyDeactivated: 'User.AlreadyDeactivated',
    AlreadyActive: 'User.AlreadyActive',
    CannotDeactivateSelf: 'User.CannotDeactivateSelf',
  },
  Outage: {
    NotFound: 'Outage.NotFound',
    EndTimeBeforeStartTime: 'Outage.EndTimeBeforeStartTime',
    StartTimeInPast: 'Outage.StartTimeInPast',
  },
  Dispatch: {
    NoStaffAvailable: 'Dispatch.NoStaffAvailable',
  },
  Validation: {
    Failed: 'Validation.Failed',
  },
  System: {
    InternalError: 'System.InternalError',
    StorageUnavailable: 'System.StorageUnavailable',
    DatabaseUnavailable: 'System.DatabaseUnavailable',
  },
} as const;

export type ErrorCode = typeof ErrorCodes[keyof typeof ErrorCodes][keyof typeof ErrorCodes[keyof typeof ErrorCodes]];
```

---

*End of Error Codes v1.0*
