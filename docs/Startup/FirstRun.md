# First Run — What Actually Works on Windows

This documents the exact steps taken to get all services running on a Windows machine.
It supersedes `localDevEnvironment.md` where the two conflict.

---

## What Is Already in the Repo / Already Works

- `apps/ResidentApp.Web/.env.local` exists (fixed: added missing `/api/v1` suffix)
- `apps/ResidentApp.Android/local.properties` exists (fixed: added `sdk.dir`)
- `apps/StaffApp.Android/local.properties` exists (fixed: added `sdk.dir`)
- EF Core `InitialCreate` migration exists and is ready to apply
- All 8 API controllers, all infrastructure services, all application handlers are complete

---

## Prerequisites

| Tool | Status on this machine |
|---|---|
| Docker Desktop 4.x | ✅ Installed |
| .NET SDK 9 (8 target) | ✅ Installed (9.x is backwards-compatible) |
| Node.js 22 LTS | ✅ Installed |
| npm 10.x | ✅ Installed |
| Android SDK | ✅ Installed at `%LOCALAPPDATA%\Android\Sdk` |
| Azure CLI (`az`) | ❌ Not installed — use Azure Storage Explorer instead |
| Azure Storage Explorer | ✅ Installed |

---

## One-Time Fixes (already applied to this repo)

### 1. Added `UserSecretsId` to `ACLS.Api.csproj`

The API project was missing `<UserSecretsId>` so `dotnet user-secrets` refused to run.
Added to `backend/ACLS.Api/ACLS.Api.csproj`:

```xml
<UserSecretsId>acls-api-secrets</UserSecretsId>
```

### 2. Created `backend/ACLS.Api/Properties/launchSettings.json`

This file was not in the repo. Without it, `ASPNETCORE_ENVIRONMENT` defaults to
`Production` and Swagger UI does not load. Created with:

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

### 3. Fixed `SubmitComplaintCommand` — missing `UnitId`

`Complaint.Create(...)` requires a `unitId` parameter but `SubmitComplaintCommand` did not
declare it. Added `int UnitId` to the command record.

### 4. Fixed `SubmitComplaintCommandValidator` — wrong `ChildRules` target

The validator applied `ChildRules` to `x.Urgency` (a `string`) instead of
`RuleForEach(x => x.MediaUrls)`. Corrected and also added proper urgency validation.

### 5. Added `Microsoft.EntityFrameworkCore.Design` to `ACLS.Api.csproj`

Required for `dotnet ef database update --startup-project ../ACLS.Api` to work.

---

## Every-New-Machine Setup (run once)

### Step 1 — Install Azurite via npm

The Docker image for Azurite (`mcr.microsoft.com/azure-storage/azurite`) failed to pull
due to a transient registry issue. Use the npm package instead — it is equivalent:

```powershell
npm install -g azurite
```

### Step 2 — Start Azurite

Run this once per development session (it must stay running while the API is active):

```powershell
$azuriteDir = "$env:USERPROFILE\.azurite"
New-Item -ItemType Directory -Force -Path $azuriteDir | Out-Null
Start-Process -FilePath "cmd.exe" `
  -ArgumentList "/c `"$env:APPDATA\npm\azurite.cmd`" --location `"$azuriteDir`"" `
  -WindowStyle Minimized
```

Verify it is listening:

```powershell
Test-NetConnection -ComputerName 127.0.0.1 -Port 10000 -InformationLevel Quiet
# Expected: True
```

### Step 3 — Create the `acls-media` blob container (once ever)

Open **Azure Storage Explorer**:

1. Expand **Emulator & Attached** → **Storage Accounts** → **(Emulator - Default Ports) (Key)**
2. Right-click **Blob Containers** → **Create Blob Container**
3. Type `acls-media` → Enter

> If Storage Explorer shows an error when expanding the emulator node, Azurite is not
> running. Start it (Step 2) then right-click the emulator node and choose **Refresh**.

### Step 4 — Configure API secrets (once ever)

```powershell
cd backend/ACLS.Api

dotnet user-secrets set "JwtSettings:Secret"                  "local-dev-secret-key-minimum-32-chars!!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet user-secrets set "ACLS_DB_CONNECTION"                  "Server=(localdb)\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet user-secrets set "ACLS_STORAGE_CONNECTION"             "UseDevelopmentStorage=true"
dotnet user-secrets set "Storage:MediaContainerName"          "acls-media"
```

> **Note on connection strings:**
> The docs in `localDevEnvironment.md` reference Docker SQL Server. On this machine a
> local SQL Server 2025 is already running on port 1433, and the EF design-time factory
> (`AclsDbContextFactory.cs`) targets `(localdb)\mssqllocaldb` / `ACLS_Dev`. Use LocalDB.
>
> **Note on storage connection string:**
> The long-form Azurite connection string causes an `Azure.Storage` parse error. Use
> `UseDevelopmentStorage=true` — it is the well-known shorthand accepted by the SDK.

### Step 5 — Configure Worker secrets (once ever)

```powershell
cd backend/ACLS.Worker

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
```

### Step 6 — Apply database migrations (once ever)

```powershell
cd backend/ACLS.Persistence
dotnet ef database update --startup-project ../ACLS.Api
```

This creates the `ACLS_Dev` database on `(localdb)\mssqllocaldb` and applies all schema
migrations. Run this again whenever new migrations are added.

### Step 7 — Install npm dependencies and build shared packages (once, or after `git pull`)

```powershell
# From the repo root
npm install
npm run build:packages
```

---

## Starting Everything (every session)

Open **four** terminals:

**Terminal 1 — Azurite (must start first)**

```powershell
$azuriteDir = "$env:USERPROFILE\.azurite"
Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$env:APPDATA\npm\azurite.cmd`" --location `"$azuriteDir`"" -WindowStyle Minimized
```

**Terminal 2 — Backend API**

```powershell
cd backend/ACLS.Api
dotnet run
```

Verify at `http://localhost:5000/healthz` — expected response: `{"status":"Healthy",...}`
Swagger UI at `http://localhost:5000/swagger`

**Terminal 3 — Background Worker**

```powershell
cd backend/ACLS.Worker
dotnet run
```

No HTTP port — confirm with console output: `Application started.`

**Terminal 4 — Web App**

```powershell
# From repo root
npm run dev
```

Open `http://localhost:3000`

---

## Verification Checklist

```powershell
# API healthy?
Invoke-WebRequest -Uri http://localhost:5000/healthz -UseBasicParsing | ConvertFrom-Json | Select-Object status

# Web app responding?
Invoke-WebRequest -Uri http://localhost:3000 -UseBasicParsing | Select-Object StatusCode

# Azurite listening?
Test-NetConnection -ComputerName 127.0.0.1 -Port 10000 -InformationLevel Quiet
```

All three should return `Healthy` / `200` / `True`.

---

## Known Issues and Workarounds

| Issue | Cause | Fix |
|---|---|---|
| `dotnet user-secrets` fails with "UserSecretsId not found" | `.csproj` was missing `<UserSecretsId>` | Already fixed in repo |
| Swagger UI returns 404 or shows Production layout | `launchSettings.json` missing | Already fixed in repo |
| Health check returns 503 (blob-storage unhealthy) | Long-form Azurite connection string rejected by `Azure.Storage` SDK | Use `UseDevelopmentStorage=true` |
| Health check returns 503 (database unhealthy) | Connection string points to Docker or wrong IP | Use LocalDB string above |
| Storage Explorer shows "no running emulator" | Azurite process not started | Run Step 2 then refresh the emulator node |
| `npm run build:packages` builds Next.js production build | `build:packages` runs all workspace builds | This is expected and harmless; `npm run dev` still starts the dev server |

---

## Android Apps

Both `local.properties` files are committed with the correct content. Open each project
in Android Studio — it will regenerate the missing `gradle-wrapper.jar` automatically on
first sync.

- `apps/ResidentApp.Android` → open in Android Studio → Run on emulator
- `apps/StaffApp.Android` → open in Android Studio → Run on emulator

The emulator uses `10.0.2.2` to reach the host machine's `localhost:5000`.
