# ACLS — Local Startup Guide

How to get every component running on a Windows machine from scratch.

---

## Prerequisites

Ensure these are installed before starting:

| Tool | Check |
|---|---|
| .NET SDK 9 (targets net8.0) | `dotnet --version` |
| Node.js 22 LTS | `node --version` |
| npm 10.x | `npm --version` |
| SQL Server (local install) | Running as a Windows service (SQL Server Configuration Manager) |
| Android Studio (for Android apps) | Open Android Studio → Help → About |

---

## Part 1 — One-Time Setup

Run these steps **once** on a new machine. Skip any step you have already completed.

---

### Step 1 — Install Azurite

The Azurite blob storage emulator is required by the backend API and Worker.

```powershell
npm install -g azurite
```

---

### Step 2 — SQL Server

SQL Server is installed locally on this machine — no Docker needed. Nothing to run here.

---

### Step 3 — Create the `acls-media` blob container

Open **Azure Storage Explorer**:

1. Expand **Emulator & Attached** → **Storage Accounts** → **(Emulator - Default Ports) (Key)**
2. Right-click **Blob Containers** → **Create Blob Container**
3. Name it `acls-media` → press Enter

> Azurite must be running first (see Part 2, Step 1 below).
> If the emulator node shows an error, start Azurite then right-click the node and choose **Refresh**.

---

### Step 4 — Configure API secrets

```powershell
cd backend/ACLS.Api

dotnet user-secrets set "JwtSettings:Secret"                  "local-dev-secret-key-minimum-32-chars!!"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
dotnet user-secrets set "ACLS_STORAGE_CONNECTION"             "UseDevelopmentStorage=true"
dotnet user-secrets set "Storage:MediaContainerName"          "acls-media"
```

---

### Step 5 — Configure Worker secrets

```powershell
cd backend/ACLS.Worker

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
```

---

### Step 6 — Apply database migrations

```powershell
cd backend/ACLS.Persistence
dotnet ef database update --startup-project ../ACLS.Api
```

Run this again whenever new migrations are added after a `git pull`.

---

### Step 7 — Install npm dependencies and build shared packages

From the **repository root**:

```powershell
cd c:\Users\divya\source\repos\ApartmentComplaintSystem
npm install
npm run build:packages
```

---

## Part 2 — Every Session

Run these steps each time you start a new development session.

---

### Step 1 — Start Azurite

Run this in a dedicated terminal and leave it open:

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

---

### Step 2 — SQL Server

SQL Server is running locally — nothing to start.

---

### Step 3 — Start the Backend (VS Code)

Open the **Run and Debug** panel in VS Code (`Ctrl+Shift+D`).

| What you want | Configuration to select |
|---|---|
| API only (with debugger) | **ACLS.Api** |
| Worker only (with debugger) | **ACLS.Worker** |
| Both at once | **API + Worker** |

Press **F5** (or click the green ▶ button) to launch.

The API builds automatically before launching. When it starts you will see a browser open at `http://localhost:5000` — confirm the health endpoint:

```powershell
Invoke-WebRequest -Uri http://localhost:5000/healthz -UseBasicParsing | ConvertFrom-Json | Select-Object status
# Expected: status = Healthy
```

Swagger UI is available at `http://localhost:5000/swagger`.

> **To run without VS Code**, use a PowerShell terminal instead:
> ```powershell
> # Terminal A — API
> cd backend/ACLS.Api
> dotnet run
>
> # Terminal B — Worker (optional, separate terminal)
> cd backend/ACLS.Worker
> dotnet run
> ```

---

### Step 4 — Start the Manager Web App

```powershell
cd c:\Users\divya\source\repos\ApartmentComplaintSystem\apps\ResidentApp.Web
npm run dev
```

Open `http://localhost:3000` in your browser.

---

## Part 3 — Android Apps

Open either app in Android Studio and press ▶ Run.

| App | Directory |
|---|---|
| Resident Android | `apps/ResidentApp.Android` |
| Staff Android | `apps/StaffApp.Android` |

The emulator routes `10.0.2.2` to your host machine's `localhost`, so the API at `http://localhost:5000` is reachable as `http://10.0.2.2:5000` from the emulator.
Both `local.properties` files are already configured with this address.

---

## Test Credentials

| Role | Email | Password |
|---|---|---|
| Resident | `alice@example.com` | `Password123!` |
| Resident | `bob@example.com` | `Password123!` |
| Resident | `carol@example.com` | `Password123!` |
| Maintenance Staff | `mike@sunsetapts.com` | `Password123!` |
| Maintenance Staff | `sarah@sunsetapts.com` | `Password123!` |

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

Expected: `Healthy` / `200` / `True`.
