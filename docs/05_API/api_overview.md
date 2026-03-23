# API Overview

**Document:** `docs/05_API/api_overview.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document and its companion `docs/05_API/openapi/v1.yaml` are the authoritative contract between the backend and all three frontend clients (React, Android, iOS). Any change to an endpoint — URL, HTTP method, request shape, response shape, status codes — must be reflected in both documents before the change is implemented in code. Clients are generated from this contract. Backend controllers must match this contract exactly.

---

## 1. Base URL and Versioning

**Strategy:** URI path versioning.  
**Current version:** `v1`  
**Base path:** `/api/v1`

All endpoints are prefixed with `/api/v1/`. A new major version (`v2`) is only cut when a breaking change cannot be avoided. Both versions are supported in parallel for a minimum of 6 months after a new version ships.

| Environment | Base URL |
|---|---|
| Local development | `http://localhost:5000/api/v1` |
| Staging | `https://acls-api-staging.azurewebsites.net/api/v1` |
| Production | `https://acls-api.azurewebsites.net/api/v1` |

---

## 2. Authentication

All endpoints except `POST /auth/login` and `POST /auth/register` require a valid JWT bearer token.

**Header format:**
```
Authorization: Bearer <token>
```

**Token contents (claims):**
| Claim | Type | Description |
|---|---|---|
| `sub` | string | `UserId` as string |
| `email` | string | User's email address |
| `role` | string | One of: `Resident`, `Manager`, `MaintenanceStaff` |
| `property_id` | string | `PropertyId` as string — the tenancy discriminator |
| `exp` | number | Expiry timestamp (Unix epoch) |
| `iss` | string | `"acls-api"` |
| `aud` | string | `"acls-clients"` |

**Token lifetime:** 60 minutes. Clients must re-authenticate after expiry.

**Role enforcement:** Controllers use `[Authorize(Roles = "...")]` attribute. The valid role combinations per endpoint are defined in Section 5 and in `v1.yaml`.

**`property_id` claim:** Extracted by `TenancyMiddleware` on every authenticated request. Never passed by clients in request bodies. See `docs/07_Implementation/patterns/multi_tenancy_pattern.md`.

---

## 3. Response Format

### 3.1 Success Responses

All successful responses return JSON with `Content-Type: application/json`.

Single resource:
```json
{
  "complaintId": 42,
  "title": "Leaking pipe under kitchen sink",
  "status": "ASSIGNED",
  "urgency": "HIGH",
  "createdAt": "2026-02-01T14:30:00Z"
}
```

Collections:
```json
{
  "items": [...],
  "totalCount": 47,
  "page": 1,
  "pageSize": 20
}
```

**All datetime fields are ISO 8601 UTC strings.** Format: `"2026-02-01T14:30:00Z"`.  
**All enum fields are uppercase strings** matching the canonical values in `docs/02_Domain/ubiquitous_language.md` (e.g. `"SOS_EMERGENCY"`, `"IN_PROGRESS"`, `"AVAILABLE"`).  
**All JSON field names are camelCase.**

### 3.2 Error Responses

All error responses follow RFC 7807 Problem Details format:

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

| Field | Description |
|---|---|
| `type` | URI identifying the error type |
| `title` | Short human-readable summary |
| `status` | HTTP status code (integer) |
| `detail` | Human-readable explanation specific to this occurrence |
| `instance` | The request URI that produced the error |
| `errorCode` | Machine-readable code matching `docs/05_API/error_codes.md` |

### 3.3 Validation Error Responses

Validation failures return HTTP 400 with an extended Problem Details body listing all field errors:

```json
{
  "type": "https://acls.api/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/complaints",
  "errorCode": "Validation.Failed",
  "errors": {
    "title": ["Title is required.", "Title must not exceed 200 characters."],
    "urgency": ["Invalid urgency value."]
  }
}
```

---

## 4. Pagination

Endpoints that return collections support cursor-free offset pagination via query parameters:

| Parameter | Type | Default | Max | Description |
|---|---|---|---|---|
| `page` | integer | `1` | — | Page number (1-based) |
| `pageSize` | integer | `20` | `100` | Items per page |

Response includes:
```json
{
  "items": [...],
  "totalCount": 47,
  "page": 1,
  "pageSize": 20
}
```

All sorting is performed by the backend. Clients never sort returned data.

---

## 5. Endpoint Reference

### 5.1 Auth

| Method | Path | Roles | Description |
|---|---|---|---|
| `POST` | `/auth/register` | Public | Register a new Resident account using an invitation token |
| `POST` | `/auth/login` | Public | Authenticate and receive a JWT |

---

### 5.2 Complaints

| Method | Path | Roles | Description |
|---|---|---|---|
| `POST` | `/complaints` | Resident | Submit a new complaint (multipart/form-data with optional media files) |
| `GET` | `/complaints` | Manager | Get all complaints for the property (filterable, paginated) |
| `GET` | `/complaints/{complaintId}` | Resident, Manager, MaintenanceStaff | Get a single complaint by ID |
| `GET` | `/complaints/my` | Resident | Get the authenticated resident's own complaints |
| `POST` | `/complaints/{complaintId}/assign` | Manager | Assign complaint to a staff member |
| `POST` | `/complaints/{complaintId}/reassign` | Manager | Reassign complaint to a different staff member |
| `POST` | `/complaints/{complaintId}/status` | MaintenanceStaff | Update complaint status (EN_ROUTE, IN_PROGRESS) |
| `POST` | `/complaints/{complaintId}/resolve` | MaintenanceStaff | Mark complaint as resolved with completion details |
| `POST` | `/complaints/{complaintId}/feedback` | Resident | Submit rating and feedback (moves status to CLOSED) |
| `POST` | `/complaints/{complaintId}/work-notes` | MaintenanceStaff | Add a work note to a complaint |
| `POST` | `/complaints/{complaintId}/eta` | MaintenanceStaff | Update estimated completion time |
| `POST` | `/complaints/sos` | Resident | Trigger SOS emergency complaint |

---

### 5.3 Staff

| Method | Path | Roles | Description |
|---|---|---|---|
| `GET` | `/staff` | Manager | Get all staff members for the property |
| `GET` | `/staff/{staffMemberId}` | Manager | Get a single staff member |
| `GET` | `/staff/available` | Manager | Get all staff with availability = AVAILABLE |
| `POST` | `/staff/{staffMemberId}/availability` | MaintenanceStaff | Update own availability status |
| `GET` | `/staff/me` | MaintenanceStaff | Get authenticated staff member's own profile and active assignments |

---

### 5.4 Dispatch

| Method | Path | Roles | Description |
|---|---|---|---|
| `GET` | `/dispatch/recommendations/{complaintId}` | Manager | Get ranked list of staff recommendations for a complaint |

---

### 5.5 Outages

| Method | Path | Roles | Description |
|---|---|---|---|
| `POST` | `/outages` | Manager | Declare a property-wide outage (triggers mass broadcast) |
| `GET` | `/outages` | Manager, Resident, MaintenanceStaff | Get all outages for the property |
| `GET` | `/outages/{outageId}` | Manager, Resident, MaintenanceStaff | Get a single outage |

---

### 5.6 Reports

| Method | Path | Roles | Description |
|---|---|---|---|
| `GET` | `/reports/dashboard` | Manager | Real-time dashboard metrics (open, in-progress, closed counts; current assignments) |
| `GET` | `/reports/staff-performance` | Manager | Staff performance summary (TAT, average rating per staff member) |
| `GET` | `/reports/unit-history/{unitId}` | Manager | All complaints for a specific unit |
| `GET` | `/reports/complaints-summary` | Manager | Filterable complaint summary report |

---

### 5.7 Users (Account Management)

| Method | Path | Roles | Description |
|---|---|---|---|
| `GET` | `/users` | Manager | Get all users for the property |
| `POST` | `/users/invite` | Manager | Generate and send an invitation token to a prospective resident |
| `POST` | `/users/{userId}/deactivate` | Manager | Deactivate a user account |
| `POST` | `/users/{userId}/reactivate` | Manager | Reactivate a deactivated user account |

---

## 6. Key Request and Response Shapes

### 6.1 Submit Complaint — `POST /complaints`

**Content-Type:** `multipart/form-data`

Request fields:
| Field | Type | Required | Notes |
|---|---|---|---|
| `title` | string | Yes | Max 200 chars |
| `description` | string | Yes | Max 2000 chars |
| `category` | string | Yes | e.g. `"Plumbing"`, `"Electrical"` |
| `urgency` | string | Yes | `LOW`, `MEDIUM`, `HIGH` (not SOS — use `/complaints/sos`) |
| `permissionToEnter` | boolean | Yes | `true` or `false` |
| `mediaFiles` | file[] | No | Up to 3 files. JPEG/PNG only. Max 5MB each |

Response `201 Created`:
```json
{
  "complaintId": 42,
  "title": "Leaking pipe under kitchen sink",
  "description": "Water is dripping steadily...",
  "category": "Plumbing",
  "urgency": "HIGH",
  "status": "OPEN",
  "permissionToEnter": true,
  "unitId": 7,
  "residentId": 3,
  "propertyId": 1,
  "media": [
    {
      "mediaId": 1,
      "url": "https://storage.acls.app/media/abc123.jpg",
      "type": "image/jpeg",
      "uploadedAt": "2026-02-01T14:30:00Z"
    }
  ],
  "workNotes": [],
  "eta": null,
  "createdAt": "2026-02-01T14:30:00Z",
  "updatedAt": "2026-02-01T14:30:00Z",
  "resolvedAt": null,
  "tat": null,
  "residentRating": null,
  "residentFeedbackComment": null,
  "assignedStaffMember": null
}
```

---

### 6.2 Assign Complaint — `POST /complaints/{complaintId}/assign`

Request body:
```json
{
  "staffMemberId": 5
}
```

Response `200 OK`: Full `ComplaintDto` with `status: "ASSIGNED"` and `assignedStaffMember` populated.

**Side effect:** `StaffMember.availability` atomically set to `"BUSY"` in the same transaction.

---

### 6.3 Resolve Complaint — `POST /complaints/{complaintId}/resolve`

**Content-Type:** `multipart/form-data`

Request fields:
| Field | Type | Required | Notes |
|---|---|---|---|
| `resolutionNotes` | string | Yes | Max 2000 chars |
| `completionPhotos` | file[] | No | Up to 3 photos |

Response `200 OK`: Full `ComplaintDto` with `status: "RESOLVED"` and `resolvedAt` populated.

**Side effects:** `StaffMember.availability` set to `"AVAILABLE"`. Resident notified via `NotificationService`.

---

### 6.4 SOS Trigger — `POST /complaints/sos`

Request body:
```json
{
  "title": "Gas leak in unit",
  "description": "Strong smell of gas in kitchen, possible leak",
  "permissionToEnter": true
}
```

Response `201 Created`: Full `ComplaintDto` with `urgency: "SOS_EMERGENCY"` and `status: "ASSIGNED"`.

**Side effects:** All on-call staff notified simultaneously via blast notification. Complaint immediately moves to `ASSIGNED` without requiring manager action.

---

### 6.5 Get Dispatch Recommendations — `GET /dispatch/recommendations/{complaintId}`

Response `200 OK`:
```json
{
  "complaintId": 42,
  "recommendations": [
    {
      "staffMemberId": 5,
      "fullName": "John Smith",
      "jobTitle": "Plumber",
      "skills": ["Plumbing", "General"],
      "availability": "AVAILABLE",
      "matchScore": 1.52,
      "skillScore": 1.0,
      "idleScore": 0.87,
      "averageRating": 4.6,
      "lastAssignedAt": "2026-01-30T09:00:00Z"
    },
    {
      "staffMemberId": 8,
      "fullName": "Maria Garcia",
      "jobTitle": "General Maintenance",
      "skills": ["General", "HVAC"],
      "availability": "AVAILABLE",
      "matchScore": 0.74,
      "skillScore": 0.5,
      "idleScore": 0.62,
      "averageRating": 4.2,
      "lastAssignedAt": "2026-01-29T15:30:00Z"
    }
  ]
}
```

The list is returned pre-sorted by `matchScore` descending. The client renders it in the order received — no client-side sorting.

---

### 6.6 Declare Outage — `POST /outages`

Request body:
```json
{
  "title": "Planned Electricity Outage",
  "outageType": "Electricity",
  "description": "Scheduled maintenance by the electricity provider. All units affected.",
  "startTime": "2026-02-10T19:00:00Z",
  "endTime": "2026-02-10T19:30:00Z"
}
```

Response `201 Created`:
```json
{
  "outageId": 3,
  "title": "Planned Electricity Outage",
  "outageType": "Electricity",
  "description": "Scheduled maintenance by the electricity provider. All units affected.",
  "startTime": "2026-02-10T19:00:00Z",
  "endTime": "2026-02-10T19:30:00Z",
  "declaredAt": "2026-02-01T14:30:00Z",
  "notificationSentAt": null
}
```

**Side effect:** Mass SMS and email broadcast triggered asynchronously to all residents. `notificationSentAt` is null until `ACLS.Worker` completes the broadcast and updates the record.

---

### 6.7 Dashboard Metrics — `GET /reports/dashboard`

Response `200 OK`:
```json
{
  "openCount": 12,
  "assignedCount": 7,
  "inProgressCount": 5,
  "resolvedCount": 3,
  "closedCount": 102,
  "sosActiveCount": 0,
  "activeAssignments": [
    {
      "complaintId": 42,
      "title": "Leaking pipe under kitchen sink",
      "urgency": "HIGH",
      "status": "IN_PROGRESS",
      "unitNumber": "4B",
      "buildingName": "Block A",
      "assignedStaffMember": {
        "staffMemberId": 5,
        "fullName": "John Smith",
        "availability": "BUSY"
      },
      "eta": "2026-02-01T16:00:00Z",
      "createdAt": "2026-02-01T14:30:00Z"
    }
  ],
  "staffAvailabilitySummary": [
    { "staffMemberId": 5, "fullName": "John Smith", "availability": "BUSY" },
    { "staffMemberId": 8, "fullName": "Maria Garcia", "availability": "AVAILABLE" }
  ]
}
```

---

## 7. Query Parameters for GET /complaints

The `GET /complaints` endpoint (Manager only) supports the following query parameters for filtering, searching, and sorting:

| Parameter | Type | Description |
|---|---|---|
| `status` | string | Filter by `TicketStatus` value |
| `urgency` | string | Filter by `Urgency` value |
| `category` | string | Filter by complaint category |
| `staffMemberId` | integer | Filter by assigned staff member |
| `unitId` | integer | Filter by unit |
| `dateFrom` | string (ISO 8601) | Filter complaints created on or after this date |
| `dateTo` | string (ISO 8601) | Filter complaints created on or before this date |
| `search` | string | Full-text search on title and description |
| `sortBy` | string | Field to sort by: `createdAt` (default), `urgency`, `status`, `updatedAt` |
| `sortDirection` | string | `asc` or `desc` (default: `desc`) |
| `page` | integer | Page number (default: 1) |
| `pageSize` | integer | Page size (default: 20, max: 100) |

All filtering and sorting is performed server-side. The response is always a paginated `ComplaintsPage` object.

---

## 8. HTTP Status Code Reference

| Code | When used |
|---|---|
| `200 OK` | Successful GET, successful POST that updates existing resource |
| `201 Created` | Successful POST that creates a new resource. `Location` header included pointing to the new resource |
| `400 Bad Request` | Validation failure. Returns Problem Details with `errors` map |
| `401 Unauthorized` | Missing or invalid JWT token |
| `403 Forbidden` | Valid token but insufficient role for the operation |
| `404 Not Found` | Resource does not exist within the authenticated user's property scope |
| `409 Conflict` | State conflict — e.g. attempting to assign an already CLOSED complaint |
| `422 Unprocessable Entity` | Request is syntactically valid but semantically invalid — e.g. invalid status transition |
| `500 Internal Server Error` | Unhandled infrastructure failure. Returns Problem Details without internal detail |

---

## 9. Media Upload Rules

- **Accepted MIME types:** `image/jpeg`, `image/png`
- **Maximum file size:** 5MB per file
- **Maximum files per complaint submission:** 3 (resident evidence)
- **Maximum completion photos per resolve:** 3 (staff completion evidence)
- **Storage:** Files are uploaded to Azure Blob Storage by `IStorageService`. Only the resulting URL is stored in the `Media` table. Binary content never enters MSSQL.
- **URL lifetime:** Blob URLs are permanent (not pre-signed with expiry) for V1. Access control is enforced at the API layer — clients cannot access blob URLs directly without going through the API.

---

## 10. Rate Limiting and Performance Targets

| NFR | Target | Endpoint scope |
|---|---|---|
| NFR-01 | 99% uptime | All endpoints |
| NFR-02 | Core pages load ≤ 2 seconds | All GET endpoints |
| NFR-12 | 500 notifications within 60 seconds | `POST /outages` broadcast side effect |

No per-client rate limiting is implemented in V1. Reviewed for V2 if abuse is observed.

---

*End of API Overview v1.0*
