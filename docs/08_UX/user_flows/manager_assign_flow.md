# Manager Assign Flow

**Document:** `docs/08_UX/user_flows/manager_assign_flow.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This document defines every screen and decision point in the Property Manager's complaint management journey in ResidentApp.Web. All data is sourced from the API and rendered as-returned — no client-side computation.

---

## 1. Login Flow

```
Browser navigates to ResidentApp.Web
    │
    ▼
Login Page (/login)
  Fields: Email, Password
  Action: POST /auth/login
    │
    ├─── Error: Auth.InvalidCredentials → Inline error message
    ├─── Error: Auth.AccountDeactivated → "Your account has been deactivated."
    │
    ▼
Dashboard Page (/dashboard)
```

---

## 2. Dashboard Flow

```
Dashboard Page (/dashboard)
  Source: GET /reports/dashboard
  Shows:
    - Metric cards: Open, Assigned, In-Progress, Resolved, Closed counts
    - SOS Active count (highlighted in red if > 0)
    - Active Assignments table:
        Columns: Complaint, Unit, Building, Urgency, Status, Assigned Staff, ETA, Age
        Sorted by: urgency desc, then createdAt asc (server-side)
    - Staff Availability summary: each staff member + AVAILABLE/BUSY/ON_BREAK badge
    │
    ▼ Tap any complaint row
    │
Complaint Detail Page (/complaints/[id])
```

---

## 3. Triage Queue Flow

```
Sidebar: Complaints
    │
    ▼
Complaints Page (/complaints)
  Source: GET /complaints (paginated)
  Filter bar:
    - Status (multi-select dropdown)
    - Urgency (multi-select dropdown)
    - Category (dropdown)
    - Date range (date pickers)
    - Search (text input — searches title and description)
  Note: All filters sent as query parameters to GET /complaints — never client-side
  Table columns: ID, Title, Unit, Urgency, Status, Submitted, Assigned Staff
    │
    ▼ Tap complaint row
    │
Complaint Detail Page (/complaints/[id])
  Source: GET /complaints/{id}
  Shows:
    - Full complaint details (title, description, category, unit, building, resident)
    - Media attachments (thumbnails, tap to fullscreen)
    - Current status + urgency badges
    - Work notes from staff
    - ETA (if set)
    - Dispatch Recommendations panel (right sidebar)
    │
    ├─── If status = OPEN or ASSIGNED:
    │        ▼ Dispatch Recommendations panel
    │        Source: GET /dispatch/recommendations/{id}
    │        Shows ranked list of staff with:
    │          - Name, job title, skills
    │          - Availability badge
    │          - Match score (not shown as number — shown as visual rank: 1st, 2nd, 3rd)
    │          - Average rating
    │        Rendered in order returned by API — no client reordering
    │        CTA per row: "Assign" button
    │            │
    │            ▼ Click Assign
    │            Confirmation dialog: "Assign [Staff Name] to this complaint?"
    │            Action: POST /complaints/{id}/assign { staffMemberId }
    │                │
    │                ├─── Error: Complaint.StaffNotAvailable → "This staff member is no longer available."
    │                ├─── Error: Complaint.InvalidStatusTransition → "This complaint cannot be assigned in its current state."
    │                │
    │                ▼
    │            Complaint status updates to ASSIGNED inline
    │
    ├─── If status = ASSIGNED or later:
    │        ▼ "Reassign" button visible
    │        Same dispatch recommendations panel appears
    │        Action: POST /complaints/{id}/reassign { staffMemberId }
```

---

## 4. Declare Outage Flow

```
Sidebar: Outages
    │
    ▼
Outages Page (/outages)
  Source: GET /outages
  Lists all declared outages (most recent first — server-side)
    │
    ▼ Click "Declare Outage" button
    │
Declare Outage Page (/outages/new)
  Fields:
    - Title (text, max 200 chars, required)
    - Outage Type (dropdown: Electricity, Water, Gas, Internet, Elevator, Other)
    - Description (text area, max 2000 chars, required)
    - Start Time (datetime picker, required)
    - End Time (datetime picker, required, must be after start time)
  Client-side validation: End time > Start time (Zod schema — field-level UX only)
  Business validation: All remaining validation is server-side
  Action: POST /outages
    │
    ├─── Error: Outage.EndTimeBeforeStartTime → "End time must be after start time."
    ├─── Error: Outage.StartTimeInPast → "Start time cannot be in the past."
    ├─── Error: Validation.Failed → Inline field errors
    │
    ▼
Outage Detail Page (/outages/[id])
  Shows declared outage details
  Shows: notificationSentAt (null initially, then populated by Worker)
  Status banner: "Notifications are being sent to all residents..." (while notificationSentAt = null)
               → "Notifications sent at [datetime]" (once notificationSentAt is populated)
```

---

## 5. Invite Resident Flow

```
Sidebar: Settings → Users
    │
    ▼
Users Page (/settings/users)
  Source: GET /users
  Table: Name, Email, Role, Unit (for Residents), Status (Active/Inactive)
  Actions per row: Deactivate / Reactivate
    │
    ▼ Click "Invite Resident"
    │
Invite Resident Page (/settings/users/invite)
  Fields:
    - Email address (required)
    - Unit (dropdown populated from property units)
  Action: POST /users/invite { email, unitId }
    │
    ├─── Error: Auth.EmailAlreadyRegistered → "An account with this email already exists."
    ├─── Error: Validation.Failed → Inline field errors
    │
    ▼
Success banner: "Invitation sent to [email]. Link expires in 72 hours."
```

---

## 6. Staff Performance View Flow

```
Sidebar: Reports
    │
    ▼
Staff Performance Page (/reports/staff)
  Source: GET /reports/staff-performance
  Table columns: Name, Job Title, Total Resolved, Average Rating (1–5 stars), Average TAT
  Note: AverageRating and AverageTat are pre-computed by ACLS.Worker — rendered as-returned
  No client-side aggregation

Unit History Page (/reports/units)
  Source: GET /reports/unit-history/{unitId} (unit selected from dropdown)
  Shows: All complaints for the selected unit, grouped by status
  Useful for identifying recurring issues in a specific apartment
```

---

*End of Manager Assign Flow v1.0*
