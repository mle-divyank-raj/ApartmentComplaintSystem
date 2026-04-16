# Local Development Environment Setup

Run this once before starting any part of the application. The backend, worker, and all
test suites depend on SQL Server and Azurite (local blob storage) being available.

---

## Prerequisites

| Tool | Minimum version | Check |
|---|---|---|
| Docker Desktop | 4.x | `docker --version` |
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 20 LTS | `node --version` |
| npm | 10.x | `npm --version` |
| Android Studio | Hedgehog (2023.1.1) | For Android apps |
| Xcode | 15+ | For iOS apps (macOS only) |

---

## 1. Start Docker Services

The application requires SQL Server 2022 and Azurite (Azure Storage emulator).
Run them as Docker containers:

```bash
# SQL Server 2022
docker run -d --name acls-sql -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# Azurite (blob + queue + table emulator)
docker run -d \
  --name acls-azurite \
  -p 10000:10000 \
  -p 10001:10001 \
  -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

Verify containers are running:

```bash
docker ps
```

---

## 2. Create the Blob Container

The API requires a blob container named `acls-media` to exist in Azurite.
Run this once after Azurite starts (requires the
[Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) or
[Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)):

```bash
# Using Azure CLI pointed at Azurite
az storage container create \
  --name acls-media \
  --connection-string "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiq4/JQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
```

---

## 3. Connection String Reference

Use these values in your local secrets files:

| Variable | Local development value |
|---|---|
| `ACLS_DB_CONNECTION` | `Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;` |
| `ACLS_STORAGE_CONNECTION` | `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiq4/JQ==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;` |
| `ACLS_STORAGE_CONTAINER` | `acls-media` |
| `ACLS_JWT_SECRET` | Any string ≥ 32 characters, e.g. `local-dev-secret-key-min-32-chars` |
| `ACLS_JWT_ISSUER` | `acls-api` |
| `ACLS_JWT_AUDIENCE` | `acls-clients` |

---

## 4. Apply Database Migrations

After SQL Server is running, apply EF Core migrations to create the schema:

```bash
cd backend/ACLS.Persistence
dotnet ef database update --startup-project ../ACLS.Api
```

The API also applies migrations automatically on startup via `db.Database.MigrateAsync()`
in `Program.cs`, so this step is optional if you're about to start the API.

---

## 5. Stopping Containers

```bash
docker stop acls-sql acls-azurite
docker rm   acls-sql acls-azurite
```

---

## Next Steps

Once the local environment is running, start each service:

- [Backend API](startBackend.md)
- [Background Worker](startWorker.md)
- [Web App](startWebApp.md)
- [Resident Android App](startResidentAndroidApp.md)
- [Staff Android App](startStaffAndroidApp.md)
- [Resident iOS App](startResidentIosApp.md)
- [Staff iOS App](startStaffIosApp.md)
