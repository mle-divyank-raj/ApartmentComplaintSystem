# Start the Resident iOS App

**Project:** `apps/ResidentApp.iOS/ResidentApp.iOS.xcodeproj`  
**Bundle ID:** `com.acls.resident`  
**Framework:** SwiftUI  
**Platform:** macOS only  
**Users:** Residents submitting and tracking maintenance complaints

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

The app reads `API_BASE_URL` from `Info.plist` at runtime via:

```swift
Bundle.main.object(forInfoDictionaryKey: "API_BASE_URL") as? String
```

The `Info.plist` already contains `http://localhost:5000/api/v1` as the default, which
works for the iOS Simulator (simulator shares the host's network stack, so `localhost`
resolves to the Mac).

**To override per build configuration** (optional), create a `Debug.xcconfig` file
at `apps/ResidentApp.iOS/Debug.xcconfig` (this file is gitignored):

```
API_BASE_URL = http://localhost:5000/api/v1/
```

Then link it to the Debug build configuration in Xcode:
**Project → Info → Configurations → Debug → set to `Debug.xcconfig`**.

> When running on a **physical iPhone**, replace `localhost` with your Mac's LAN IP
> address, e.g. `http://192.168.1.10:5000/api/v1`.

---

## Step 2 — Open the Project in Xcode

```bash
open apps/ResidentApp.iOS/ResidentApp.iOS.xcodeproj
```

Or from Xcode: **File → Open → `apps/ResidentApp.iOS/ResidentApp.iOS.xcodeproj`**.

---

## Step 3 — Select a Simulator and Run

1. In Xcode's toolbar, select a simulator from the device picker (e.g. **iPhone 16**).
2. Press **Command+R** or click the **Run** button (▶).

Xcode will build the app and launch it in the simulator automatically.

---

## Building from the Command Line

```bash
xcodebuild \
  -project apps/ResidentApp.iOS/ResidentApp.iOS.xcodeproj \
  -scheme ResidentApp.iOS \
  -configuration Debug \
  -destination "platform=iOS Simulator,name=iPhone 16" \
  build
```

---

## App Permissions

The app requests the following permissions at runtime (declared in `Info.plist`):

| Permission | Reason |
|---|---|
| Photo Library | Select photos to attach to a complaint |
| Camera | Take photos to attach to a complaint |

The iOS Simulator will prompt for these on first use — tap **Allow**.

---

## Resident App Screens

Implements all screens from `docs/08_UX/resident_complaint_flow.md`:

| Screen | SwiftUI View |
|---|---|
| Login | `LoginView` |
| Register (invitation token) | `RegisterView` |
| Submit Complaint | `SubmitComplaintView` |
| My Complaints list | `ComplaintListView` |
| Complaint Detail (timeline, ETA, notes) | `ComplaintDetailView` |
| SOS Emergency | `SosView` |
| Submit Feedback | `FeedbackView` |
| Outage List | `OutageListView` |

---

## Troubleshooting

**Build fails: `No such module 'X'`**  
Clean the build folder: **Product → Clean Build Folder** (`Shift+Command+K`), then rebuild.

**`Connection refused` or network errors in simulator**  
- Confirm the backend is running: `curl http://localhost:5000/healthz` in Terminal.
- The simulator uses the Mac's localhost — `localhost` in `API_BASE_URL` is correct.

**Camera not available in simulator**  
The iOS Simulator does not have a physical camera. Use the photo library option or run on
a physical device for camera-related flows.

**Signing errors when running on a physical device**  
In Xcode: **Signing & Capabilities → Team** — select your Apple Developer account.
