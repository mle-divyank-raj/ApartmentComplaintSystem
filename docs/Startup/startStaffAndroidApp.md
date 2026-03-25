# Start the Staff Android App

**Project:** `apps/StaffApp.Android/`  
**Package ID:** `com.acls.staff`  
**Framework:** Kotlin / Jetpack Compose / Hilt  
**Users:** Maintenance staff viewing assignments, updating status, and resolving complaints

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Android Studio | Hedgehog (2023.1.1) or later | Recommended IDE |
| Java | 17 | Set via `JAVA_HOME` or Android Studio's bundled JDK |
| Android SDK | API 35 | Target SDK |
| Android Emulator | API 26+ | `minSdk` is 26 |

Make sure the Backend API is running — see [Start the Backend](startBackend.md).

---

## Step 1 — Create `local.properties`

Create `apps/StaffApp.Android/local.properties` (this file is gitignored):

```properties
sdk.dir=/path/to/your/Android/sdk
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

> `10.0.2.2` is the Android emulator's alias for `localhost` on the host machine.
> Replace with your host machine's LAN IP when using a physical device,
> e.g. `http://192.168.1.10:5000/api/v1/`.

**Finding your SDK path:**
- Android Studio: **File → Project Structure → SDK Location**
- macOS default: `~/Library/Android/sdk`
- Windows default: `%LOCALAPPDATA%\Android\Sdk`

---

## Step 2 — Open in Android Studio

```bash
# Open the project from the command line:
studio apps/StaffApp.Android

# Or open Android Studio and use: File → Open → apps/StaffApp.Android
```

> The Resident and Staff apps are **separate Gradle projects**. Open them in separate
> Android Studio windows — do not open them as submodules of each other.

---

## Step 3 — Run the App

### Option A — From Android Studio

1. Select a device or emulator from the device dropdown.
2. Click **Run** or press `Shift+F10`.

### Option B — From the command line

```bash
cd apps/StaffApp.Android

./gradlew installDebug
adb shell am start -n com.acls.staff/.MainActivity
```

---

## Gradle Command Reference

| Command | Description |
|---|---|
| `./gradlew assembleDebug` | Build a debug APK (output: `app/build/outputs/apk/debug/`) |
| `./gradlew assembleRelease` | Build a release APK (requires signing config) |
| `./gradlew installDebug` | Build + install on connected device/emulator |
| `./gradlew test` | Run unit tests |
| `./gradlew connectedAndroidTest` | Run instrumented tests on a connected device/emulator |
| `./gradlew clean` | Delete build outputs |

---

## Staff App Screens

The app implements all screens from `docs/08_UX/staff_resolve_flow.md`:

| Screen | Route / ViewModel |
|---|---|
| Login | `LoginScreen` / `LoginViewModel` |
| My Tasks (assigned complaints) | `MyTasksScreen` / `MyTasksViewModel` |
| Task Detail | `TaskDetailScreen` / `TaskDetailViewModel` |
| Resolve Complaint (photo upload) | `ResolveScreen` / `ResolveViewModel` |
| Availability Toggle | `AvailabilityScreen` / `AvailabilityViewModel` |

---

## Troubleshooting

**`Connection refused` when calling the API**  
- Confirm the backend: `curl http://localhost:5000/healthz` on the host.
- Use `10.0.2.2` in `local.properties` for emulators, or the host LAN IP for physical devices.

**`JAVA_HOME` error during Gradle build**  
Go to **File → Project Structure → SDK Location → Gradle JDK** in Android Studio and select JDK 17.

**Gradle sync fails after pulling changes**  
Run `./gradlew clean`, then **File → Sync Project with Gradle Files** in Android Studio.
