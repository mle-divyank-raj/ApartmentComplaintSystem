# Missing Pieces — What Is Needed to Run Each Component

Audit performed: 2026-03-25. Based on actual file inspection of the repository.

---

## Status Summary

| Component | Runnable now? | Blocker(s) |
|---|---|---|
| **Web App** | ✅ Yes | None |
| **Resident Android** | ⚠️ Almost | `local.properties` must be created; `gradle-wrapper.jar` not committed |
| **Staff Android** | ⚠️ Almost | Same as above |
| **Resident iOS** | ✅ Yes (macOS only) | Nothing confirmed missing |
| **Staff iOS** | ✅ Yes (macOS only) | Nothing confirmed missing |
| **Backend API** | ❌ No | `JwtSettings:Secret` hard crash; no DB; no `launchSettings.json` |
| **Background Worker** | ❌ No | No DB connection configured |
| **Any component needing real data** | ❌ No | No `docker-compose.yml`; no seed script |

---

## 1. Backend API

### Missing: `JwtSettings:Secret` — hard crash on startup

`Program.cs` throws immediately if this key is absent:

```csharp
var jwtSecret = configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
```

**Fix — run these once from a terminal:**

```bash
cd backend/ACLS.Api

dotnet user-secrets set "JwtSettings:Secret"                  "any-string-minimum-32-chars-local!!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
dotnet user-secrets set "ACLS_STORAGE_CONNECTION"             "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiq4/JQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
dotnet user-secrets set "Storage:MediaContainerName"          "acls-media"
```

All four keys are required. Missing any one of them will either crash the process or fail
the startup health check.

---

### Missing: `backend/ACLS.Api/Properties/launchSettings.json`

The `Properties/` folder does not exist. Without `launchSettings.json`:

- `ASPNETCORE_ENVIRONMENT` does not default to `Development` — Swagger UI will not load.
- No dev port is declared — Kestrel falls back to port 5000 (HTTP) / 5001 (HTTPS).

**Fix — create the file at `backend/ACLS.Api/Properties/launchSettings.json`:**

```json
{
  "profiles": {
    "ACLS.Api": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

### Missing: SQL Server and Azurite (Docker)

No `docker-compose.yml` exists anywhere in the repository. SQL Server and Azurite must be
started manually before the API can connect to the database or blob storage.

**Fix — run these Docker commands once per development session:**

```bash
# SQL Server 2022
docker run -d --name acls-sql \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# Azurite (blob storage emulator)
docker run -d --name acls-azurite \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

See [localDevEnvironment.md](localDevEnvironment.md) for the full setup including blob
container creation.

---

### What Is Already Present (no action needed)

- All 8 controllers exist and are complete.
- `ACLS.Infrastructure` is fully implemented (`BlobStorageService`, `NotificationService`,
  `DispatchService`, `JwtTokenService`, `BcryptPasswordHasher`, `ReportingService`).
- `InfrastructureServiceCollectionExtensions.cs` registers all infrastructure services.
- `AclsDbContext` implements `IUnitOfWork`.
- EF Core `InitialCreate` migration exists — schema is ready to apply.
- `ExceptionHandlingMiddleware` and `TenancyMiddleware` exist and are wired in `Program.cs`.

---

## 2. Background Worker

### Missing: DB connection string

The Worker has no `appsettings.json` and no User Secrets initialised.

**Fix:**

```bash
cd backend/ACLS.Worker

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
```

Notification credentials are optional in development (the Worker will skip actual
email/SMS delivery without them):

```bash
dotnet user-secrets set "ACLS_NOTIFICATION_KEY"        "your-provider-api-key"
dotnet user-secrets set "ACLS_NOTIFICATION_EMAIL_FROM" "noreply@acls.app"
dotnet user-secrets set "ACLS_NOTIFICATION_SMS_FROM"   "+15550000000"
```

---

## 3. Web App

### Nothing is missing

`.env.local` already exists with:
```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

Build confirmed clean (TypeScript + Next.js). Run:

```bash
npm install
npm run build:packages
npm run dev        # → http://localhost:3000
```

---

## 4. Android Apps (Resident + Staff)

### Missing: `local.properties` (both apps)

This file is gitignored and must be created by each developer. It is required for Gradle
to locate the Android SDK and for `BuildConfig.API_BASE_URL` to be injected at build time.

**Fix — create `apps/ResidentApp.Android/local.properties`:**

```properties
sdk.dir=C\:\\Users\\divya\\AppData\\Local\\Android\\Sdk
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

**Fix — create `apps/StaffApp.Android/local.properties`:**

```properties
sdk.dir=C\:\\Users\\divya\\AppData\\Local\\Android\\Sdk
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

> `10.0.2.2` is the Android emulator's loopback alias for the host machine's `localhost`.
> Replace with your host LAN IP when using a physical device.

---

### Missing: `gradle-wrapper.jar`

The Gradle wrapper JAR is not committed to the repository. Android Studio regenerates it
automatically on first project open. From the command line:

```bash
# Option A — open in Android Studio and let it sync (recommended)

# Option B — if Gradle is installed globally
cd apps/ResidentApp.Android
gradle wrapper

cd apps/StaffApp.Android
gradle wrapper
```

After the wrapper is in place, use `./gradlew` (macOS/Linux) or `gradlew.bat` (Windows)
for all builds.

---

## 5. iOS Apps (Resident + Staff)

### No blockers confirmed

Both Xcode projects exist and contain all source files. No `Podfile` or `Package.swift`
was found, indicating no third-party dependencies — pure SwiftUI + Foundation. The projects
should open and build in Xcode without additional setup.

`Info.plist` in both apps already contains `API_BASE_URL = http://localhost:5000/api/v1`,
which is the correct value for the iOS Simulator (simulator shares the Mac's network
stack, so `localhost` resolves to the host).

---

## 6. No Dev Environment Automation

The `tools/` directory does not exist. There is no:

| Missing file | What it would do |
|---|---|
| `tools/dev-environment/docker-compose.yml` | Start SQL Server + Azurite + MailHog with a single command |
| `tools/dev-environment/bootstrap.sh` | One-shot environment setup for a new machine |
| `tools/scripts/seed-db.ps1` | Populate the database with test properties, users, and complaints |

Until these are created, every developer must set up Docker containers and secrets
manually as described in [localDevEnvironment.md](localDevEnvironment.md) and this document.

---

## Quick-Start Checklist

Work through this list in order to get the full stack running locally:

- [ ] Docker Desktop is running
- [ ] `docker run` SQL Server on port 1433 (see Section 1)
- [ ] `docker run` Azurite on port 10000 (see Section 1)
- [ ] `az storage container create --name acls-media ...` against Azurite
- [ ] `dotnet user-secrets set` × 4 in `backend/ACLS.Api` (see Section 1)
- [ ] `dotnet user-secrets set` × 1 in `backend/ACLS.Worker` (see Section 2)
- [ ] Create `backend/ACLS.Api/Properties/launchSettings.json` (see Section 1)
- [ ] `cd backend/ACLS.Api && dotnet run` — verify `GET /healthz` returns `Healthy`
- [ ] `cd backend/ACLS.Worker && dotnet run` — verify host started log appears
- [ ] `npm install && npm run build:packages && npm run dev` — verify `http://localhost:3000` loads
- [ ] Create `apps/ResidentApp.Android/local.properties` and open in Android Studio
- [ ] Create `apps/StaffApp.Android/local.properties` and open in Android Studio
- [ ] Open `apps/ResidentApp.iOS/ResidentApp.iOS.xcodeproj` in Xcode and run (macOS only)
- [ ] Open `apps/StaffApp.iOS/StaffApp.iOS.xcodeproj` in Xcode and run (macOS only)
