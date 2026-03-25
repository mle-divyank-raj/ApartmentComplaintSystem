# Start the Backend API

**Project:** `backend/ACLS.Api/ACLS.Api.csproj`  
**Framework:** .NET 8 / ASP.NET Core  
**Default URL:** `http://localhost:5000`

---

## Prerequisites

- .NET 8 SDK installed — `dotnet --version`
- SQL Server and Azurite running locally — see [Local Dev Environment](localDevEnvironment.md)
- Secrets configured (step 1 below)

---

## Step 1 — Configure Secrets

The API reads sensitive configuration from environment variables or .NET User Secrets.
User Secrets are the recommended approach for local development.

```bash
cd backend/ACLS.Api

dotnet user-secrets set "ACLS_DB_CONNECTION"       "Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
dotnet user-secrets set "ACLS_STORAGE_CONNECTION"  "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiq4/JQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
dotnet user-secrets set "ACLS_STORAGE_CONTAINER"   "acls-media"
dotnet user-secrets set "ACLS_JWT_SECRET"          "local-dev-secret-key-minimum-32-chars!!"
dotnet user-secrets set "ACLS_JWT_ISSUER"          "acls-api"
dotnet user-secrets set "ACLS_JWT_AUDIENCE"        "acls-clients"
```

Alternatively, set them as environment variables before running:

```powershell
# PowerShell
$env:ACLS_DB_CONNECTION      = "Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
$env:ACLS_STORAGE_CONNECTION = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiq4/JQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
$env:ACLS_STORAGE_CONTAINER  = "acls-media"
$env:ACLS_JWT_SECRET         = "local-dev-secret-key-minimum-32-chars!!"
$env:ACLS_JWT_ISSUER         = "acls-api"
$env:ACLS_JWT_AUDIENCE       = "acls-clients"
```

---

## Step 2 — Restore and Run

```bash
cd backend/ACLS.Api

dotnet restore
dotnet run
```

The API will:
1. Apply any pending EF Core migrations automatically on startup.
2. Start listening on `http://localhost:5000`.

To run in watch mode (auto-restarts on file changes):

```bash
dotnet watch run
```

---

## Step 3 — Verify

| Check | URL |
|---|---|
| Health check | `GET http://localhost:5000/healthz` |
| Swagger UI (Development only) | `http://localhost:5000/swagger` |

A healthy response from `/healthz` looks like:

```json
{ "status": "Healthy" }
```

---

## All Configuration Keys

| Key | Required | Default | Notes |
|---|---|---|---|
| `ACLS_DB_CONNECTION` | Yes | — | Full SQL Server connection string |
| `ACLS_STORAGE_CONNECTION` | Yes | — | Azurite or Azure Blob connection string |
| `ACLS_STORAGE_CONTAINER` | Yes | — | Blob container name (`acls-media` locally) |
| `ACLS_JWT_SECRET` | Yes | — | HMAC-SHA256 key, minimum 32 characters |
| `ACLS_JWT_ISSUER` | No | `acls-api` | JWT `iss` claim |
| `ACLS_JWT_AUDIENCE` | No | `acls-clients` | JWT `aud` claim |
| `ACLS_JWT_EXPIRY_MINUTES` | No | `60` | Token lifetime in minutes |
| `ACLS_APPINSIGHTS_CONNECTION` | No | — | Staging/Production only; ignored in Development |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | Set to `Development` to enable Swagger and debug logs |

---

## Running the Full Backend Together

Run the API and the Worker at the same time in separate terminals:

```bash
# Terminal 1
cd backend/ACLS.Api && dotnet run

# Terminal 2
cd backend/ACLS.Worker && dotnet run
```

See [Start the Worker](startWorker.md) for worker-specific setup.
