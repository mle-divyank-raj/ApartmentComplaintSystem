# Staff Resolve Flow

**Document:** `docs/08_UX/user_flows/staff_resolve_flow.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This document defines every screen and transition in the Maintenance Staff journey across StaffApp.Android and StaffApp.iOS. All data is sourced from the API and rendered as-returned.

---

## 1. Login Flow

```
App launch
    │
    ▼
Login Screen
  Fields: Email, Password
  Action: POST /auth/login { email, password }
    │
    ├─── Error: Auth.InvalidCredentials → "Incorrect email or password."
    ├─── Error: Auth.AccountDeactivated → "Your account has been deactivated."
    │
    ▼
My Tasks Screen (Home)
```

---

## 2. View My Tasks Flow

```
My Tasks Screen (Home)
  Source: GET /staff/me
  Shows:
    - Own availability toggle (Available / On Break / Off Duty)
    - Active assignments list — sorted by urgency desc (server-side)
    - Each row: complaint title, unit number, building, urgency badge, status badge, ETA
    │
    ▼ Tap task
    │
Task Detail Screen
  Source: GET /complaints/{id}
  Shows:
    - Title, description, category
    - Unit number, floor, building name
    - Permission to enter indicator
    - Resident name (first name only for privacy)
    - Current status badge
    - ETA (editable inline)
    - Work notes (own notes, oldest first)
    - Media attachments from resident
    - Action buttons (context-sensitive — see flows below)
```

---

## 3. Accept Assignment Flow

```
Task Detail Screen (status = ASSIGNED)
    │
    ▼ Tap "Accept & En Route"
    │
    Action: POST /complaints/{id}/status { status: "EN_ROUTE" }
    │
    ├─── Error: Complaint.InvalidStatusTransition → "This complaint is no longer in ASSIGNED status."
    │
    ▼
Status updates to EN_ROUTE inline
ETA field becomes required: "Set estimated arrival time"
    │
    ▼ Staff sets ETA
    Action: POST /complaints/{id}/eta { eta: "2026-02-01T16:00:00Z" }
    (Triggers resident notification via Worker)
```

---

## 4. Start Work Flow

```
Task Detail Screen (status = EN_ROUTE)
    │
    ▼ Tap "Start Work"
    │
    Action: POST /complaints/{id}/status { status: "IN_PROGRESS" }
    │
    ▼
Status updates to IN_PROGRESS inline
  "Update ETA" button visible
  "Add Work Note" button visible
```

---

## 5. Add Work Note Flow

```
Task Detail Screen (status = IN_PROGRESS or EN_ROUTE)
    │
    ▼ Tap "Add Note"
    │
Add Note Sheet (bottom sheet)
  Field: Note content (text area, max 2000 chars)
  Action: POST /complaints/{id}/work-notes { content }
    │
    ▼
Note appears in task detail work notes list
```

---

## 6. Update ETA Flow

```
Task Detail Screen (status = EN_ROUTE or IN_PROGRESS)
    │
    ▼ Tap "Update ETA"
    │
ETA Picker Sheet
  Date and time picker — UTC time displayed in local timezone
  Action: POST /complaints/{id}/eta { eta }
    │
    ▼
ETA updated inline
Resident automatically notified of new ETA (by Worker — staff does not trigger this directly)
```

---

## 7. Resolve Complaint Flow

```
Task Detail Screen (status = IN_PROGRESS)
    │
    ▼ Tap "Mark Resolved"
    │
Resolve Sheet (bottom sheet or full screen)
  Fields:
    - Resolution notes (text area, max 2000 chars, required)
    - Completion photos (camera/gallery, optional, up to 3)
  Action: POST /complaints/{id}/resolve (multipart/form-data)
    │
    ├─── Error: Complaint.InvalidStatusTransition → "This complaint is not in progress."
    ├─── Error: Complaint.MaxMediaAttachmentsExceeded → "Maximum 3 completion photos."
    │
    ▼
Task removed from My Tasks list
Status shows RESOLVED
Resident notified automatically (by Worker)
Availability automatically set to AVAILABLE by system
```

---

## 8. Update Availability Flow

```
My Tasks Screen
    │
    ▼ Tap availability toggle
    │
Availability Picker
  Options: Available | On Break | Off Duty
  Note: "Busy" is not selectable — set automatically by system when assigned
  Action: POST /staff/{staffMemberId}/availability { availability }
    │
    ├─── Error: Staff.CannotSetBusyManually → "Busy status is set automatically when you are assigned a complaint."
    │
    ▼
Availability badge updates inline
```

---

*End of Staff Resolve Flow v1.0*
