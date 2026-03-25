# Start the Resident Android App

**Project:** `apps/ResidentApp.Android/`  
**Package ID:** `com.acls.resident`  
**Framework:** Kotlin / Jetpack Compose / Hilt  
**Users:** Residents submitting and tracking maintenance complaints

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

Create `apps/ResidentApp.Android/local.properties` (this file is gitignored).
It must contain the path to your Android SDK and the backend API URL:

```properties
sdk.dir=/path/to/your/Android/sdk
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

> `10.0.2.2` is the Android emulator's alias for `localhost` on the host machine.
> If running on a physical device connected to the same network, replace with your
> machine's local IP address, e.g. `http://192.168.1.10:5000/api/v1/`.

**Finding your SDK path:**
- Android Studio: **File → Project Structure → SDK Location**
- macOS default: `~/Library/Android/sdk`
- Windows default: `%LOCALAPPDATA%\Android\Sdk`

---

## Step 2 — Open in Android Studio

```bash
# Open the project from the command line:
studio apps/ResidentApp.Android

# Or open Android Studio and use: File → Open → apps/ResidentApp.Android
```

Android Studio will sync the Gradle files automatically on first open.

---

## Step 3 — Run the App

### Option A — From Android Studio

1. Select a device or emulator from the device dropdown (top toolbar).
2. Click the **Run** button (green triangle) or press `Shift+F10`.

### Option B — From the command line

```bash
cd apps/ResidentApp.Android

# Build and install on a connected emulator or device:
./gradlew installDebug

# Then launch the app (replace com.acls.resident with the actual launcher activity if needed):
adb shell am start -n com.acls.resident/.MainActivity
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

## Troubleshooting

**`JAVA_HOME` not set or wrong version**  
In Android Studio, go to **File → Project Structure → SDK Location → Gradle Settings**
and set the Gradle JDK to the bundled JDK (version 17).

**`Connection refused` when calling the API**  
- Confirm the backend is running: `curl http://localhost:5000/healthz` on the host.
- Confirm `local.properties` uses `10.0.2.2` (emulator) not `localhost`.
- If using a physical device, use your host machine's LAN IP.

**Gradle sync fails after pulling changes**  
Run `./gradlew clean` then re-sync from Android Studio (**File → Sync Project with Gradle Files**).
