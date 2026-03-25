# Start the Staff iOS App

**Project:** `apps/StaffApp.iOS/StaffApp.iOS.xcodeproj`  
**Bundle ID:** `com.acls.staff`  
**Framework:** SwiftUI  
**Platform:** macOS only  
**Orientation:** Portrait only  
**Users:** Maintenance staff viewing assignments, updating status, and attaching completion photos

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| macOS | Ventura (13) or later | Required to run Xcode |
| Xcode | 15 or later | Install from the Mac App Store |
| iOS Simulator | iOS 17+ | Bundled with Xcode |
| Xcode Command Line Tools | Latest | `xcode-select --install` |

Make sure the Backend API is running — see [Start the Backend](startBackend.md).

---

## Step 1 — Configure the API URL

The app reads `API_BASE_URL` from `Info.plist` at runtime. The default value is
`http://localhost:5000/api/v1`, which works for the iOS Simulator.

**To override per build configuration** (optional), create `apps/StaffApp.iOS/Debug.xcconfig`
(gitignored):

```
API_BASE_URL = http://localhost:5000/api/v1/
```

Link it in Xcode: **Project → Info → Configurations → Debug → set to `Debug.xcconfig`**.

> When running on a **physical iPhone**, replace `localhost` with your Mac's LAN IP,
> e.g. `http://192.168.1.10:5000/api/v1`.

---

## Step 2 — Open the Project in Xcode

```bash
open apps/StaffApp.iOS/StaffApp.iOS.xcodeproj
```

Or from Xcode: **File → Open → `apps/StaffApp.iOS/StaffApp.iOS.xcodeproj`**.

> The Resident and Staff iOS apps are **separate Xcode projects**. Open them in
> separate Xcode windows.

---

## Step 3 — Select a Simulator and Run

1. Select a simulator from the device picker (e.g. **iPhone 16**).
2. Press **Command+R** or click **Run** (▶).

---

## Building from the Command Line

```bash
xcodebuild \
  -project apps/StaffApp.iOS/StaffApp.iOS.xcodeproj \
  -scheme StaffApp.iOS \
  -configuration Debug \
  -destination "platform=iOS Simulator,name=iPhone 16" \
  build
```

---

## App Permissions

| Permission | Reason |
|---|---|
| Photo Library | Attach completion photos when resolving a complaint |
| Camera | Take a photo to attach when resolving a complaint |

---

## Staff App Screens

Implements all screens from `docs/08_UX/staff_resolve_flow.md`:

| Screen | SwiftUI View |
|---|---|
| Login | `LoginView` |
| My Tasks (assigned complaints) | `TaskListView` |
| Task Detail | `TaskDetailView` |
| Resolve Complaint (photo + notes) | `ResolveView` |
| Availability Toggle | `AvailabilityView` |

Navigation is managed by `NavigationRouter.swift`.

---

## Troubleshooting

**Build fails: `No such module 'X'`**  
Clean the build folder: **Product → Clean Build Folder** (`Shift+Command+K`), then rebuild.

**`Connection refused` in simulator**  
- Confirm the backend is running: `curl http://localhost:5000/healthz`.
- `localhost` maps to the Mac from the simulator — no extra configuration needed.

**App opens but stays on the login screen after tapping Login**  
Ensure a staff user account exists in the database. A Manager can invite staff via the
Web App, or seed the database directly using `tools/scripts/seed-db.ps1`.

**Signing errors on physical device**  
In Xcode: **Signing & Capabilities → Team** — select your Apple Developer account.
