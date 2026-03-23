# Resident Complaint Flow

**Document:** `docs/08_UX/user_flows/resident_complaint_flow.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This document defines every screen and transition in the Resident journey across ResidentApp.Android and ResidentApp.iOS. Screen content is display-only — no business logic on the client.

---

## 1. Registration Flow

```
Invitation email received
    │
    ▼
Open invitation link
    │
    ▼
Registration Screen
  Fields: First Name, Last Name, Email (pre-filled from token), Password, Phone (optional)
  Action: POST /auth/register { invitationToken, email, password, firstName, lastName }
    │
    ├─── Error: Auth.InvitationTokenExpired → "This invitation has expired. Contact your property manager."
    ├─── Error: Auth.EmailAlreadyRegistered → "An account with this email already exists."
    │
    ▼
Home Screen (authenticated)
```

---

## 2. Login Flow

```
App launch
    │
    ▼
Login Screen
  Fields: Email, Password
  Action: POST /auth/login { email, password }
    │
    ├─── Error: Auth.InvalidCredentials → "Incorrect email or password."
    ├─── Error: Auth.AccountDeactivated → "Your account has been deactivated. Contact your property manager."
    │
    ▼
Home Screen
```

---

## 3. Submit Complaint Flow

```
Home Screen
    │
    ▼ Tap "New Complaint"
    │
    ▼
Complaint Form Screen
  Fields:
    - Category (picker: Plumbing, Electrical, HVAC, Structural, Pest, Other)
    - Title (text, max 200 chars)
    - Description (text area, max 2000 chars)
    - Urgency (picker: Low, Medium, High)
    - Permission to Enter (toggle: Yes / No)
    - Photos (up to 3 — camera or gallery)
  Action: POST /complaints (multipart/form-data)
    │
    ├─── Error: Validation.Failed → Show inline field errors
    ├─── Error: Complaint.MaxMediaAttachmentsExceeded → "Maximum 3 photos allowed."
    ├─── Error: Complaint.InvalidMediaType → "Only JPEG and PNG photos are accepted."
    ├─── Error: Complaint.MediaFileTooLarge → "Each photo must be under 5MB."
    │
    ▼
Confirmation Screen
  Shows: Complaint ID, title, submitted timestamp
  CTA: "View Status" → Complaint Detail Screen
```

---

## 4. SOS Emergency Flow

```
Home Screen
    │
    ▼ Tap SOS button (distinct red panic button)
    │
    ▼
SOS Confirmation Dialog
  "This will immediately alert all on-call maintenance staff.
   Only use for genuine emergencies (fire, flooding, gas leak)."
  Actions: CANCEL | CONFIRM EMERGENCY
    │
    ▼ CONFIRM EMERGENCY
    │
SOS Form Screen
  Fields: Title (pre-filled: "Emergency"), Description, Permission to Enter
  Action: POST /complaints/sos
    │
    ▼
SOS Confirmed Screen
  Shows: "Emergency reported. All on-call staff have been alerted."
  Complaint status: ASSIGNED (shown immediately)
```

---

## 5. Track Complaint Status Flow

```
Home Screen or Notification tap
    │
    ▼
My Complaints Screen
  Lists all complaints for this resident, sorted by createdAt desc (server-side)
  Shows: title, status badge, urgency badge, createdAt
  Source: GET /complaints/my
    │
    ▼ Tap complaint
    │
Complaint Detail Screen
  Shows:
    - Title, description, category
    - Status timeline: Submitted → Assigned → En Route → In Progress → Resolved
    - Urgency badge
    - ETA (if set by staff): "Estimated completion: [datetime]"
    - Assigned staff member name and job title
    - Media thumbnails (tappable to fullscreen)
    - Work notes (visible to resident — freetext from staff)
    - TAT (shown after resolution: "Resolved in X hours Y minutes")
  Source: GET /complaints/{id}
    │
    ├─── If status = RESOLVED and no feedback submitted:
    │        ▼
    │    Feedback prompt banner: "How did we do? Rate this repair"
    │        ▼ Tap
    │    Feedback Screen (see flow 6)
    │
    ├─── If status = CLOSED:
    │        Show submitted rating and feedback comment (read-only)
```

---

## 6. Feedback Flow

```
Complaint Detail Screen (status = RESOLVED)
    │
    ▼ Tap "Leave Feedback"
    │
Feedback Screen
  Shows complaint title and resolving staff member name
  Fields:
    - Star rating (1–5, required)
    - Comment (text area, optional, max 1000 chars)
  Action: POST /complaints/{id}/feedback { rating, comment }
    │
    ├─── Error: Complaint.FeedbackAlreadySubmitted → "You've already left feedback for this complaint."
    ├─── Error: Complaint.FeedbackNotAllowed → "Feedback can only be submitted after the complaint is resolved."
    │
    ▼
Thank You Screen
  "Thank you for your feedback. Your complaint is now closed."
  Complaint status updated to CLOSED in UI
```

---

## 7. Outage Notification Receive Flow

```
Push notification / SMS received: "Property-wide outage declared"
    │
    ▼ Tap notification
    │
Outage Detail Screen
  Shows: Title, outage type, description, start time, end time
  Source: GET /outages/{id}
    │
Outages List accessible from Home Screen
  Source: GET /outages (most recent first)
```

---

*End of Resident Complaint Flow v1.0*
