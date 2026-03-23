# Environment Configuration

**Document:** `docs/09_Operations/environment_config.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document is the authoritative reference for all environment variables, configuration keys, and secrets used across ACLS. No secret may be hardcoded anywhere. No new configuration key may be introduced without being documented here first. All environment variable names in code must match this document exactly.

---

## 1. Environments

| Environment | Purpose | Database | Deployment |
|---|---|---|---|
| `Development` (local) | Developer workstation — Docker SQL Server, Azurite | Docker `mcr.microsoft.com/mssql/server:2022-latest` | Manual (`dotnet run`) |
| `Staging` | Pre-production integration testing | Azure SQL (Standard S2 tier) | GitHub Actions on merge to `main` |
| `Production` | Live system | Azure SQL (Standard S3 or Premium tier) | GitHub Actions with manual approval gate |

`ASPNETCORE_ENVIRONMENT` is set to `Development`, `Staging`, or `Production` on the host. The application reads this to determine which `appsettings.<env>.json` overrides to apply.

---

## 2. Secret Management by Environment

| Environment | Where secrets live |
|---|---|
| `Development` | `appsettings.Development.json` (gitignored) |
| `Staging` | Azure Key Vault `acls-keyvault-staging` → injected as environment variables by App Service |
| `Production` | Azure Key Vault `acls-keyvault-prod` → injected as environment variables by App Service |
| CI/CD pipelines | GitHub Actions Secrets (repository level) |

**Absolute rule:** No secret value is ever committed to source control. `appsettings.json` (committed) contains only empty string placeholders. `appsettings.Development.json` is listed in `.gitignore`.

---

## 3. Environment Variable Reference

These are the exact variable names used in code via `builder.Configuration["KEY_NAME"]`. All names are SCREAMING_SNAKE_CASE.

### 3.1 Database

| Variable | Used in | Description |
|---|---|---|
| `ACLS_DB_CONNECTION` | `ACLS.Persistence/DependencyInjection.cs` | Full SQL Server connection string |

**Development value example** (in `appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ACLS_Dev;User Id=sa;Password=YourLocalPassword!;TrustServerCertificate=True;"
  }
}
```
Note: the code reads `ConnectionStrings:DefaultConnection` in development, mapped to `ACLS_DB_CONNECTION` via the hosting environment in staging/production.

### 3.2 Authentication

| Variable | Used in | Description |
|---|---|---|
| `ACLS_JWT_SECRET` | `ACLS.Api/Program.cs` | HMAC-SHA256 signing key. Min 32 characters. |
| `ACLS_JWT_ISSUER` | `ACLS.Api/Program.cs` | JWT issuer claim. Value: `"acls-api"` |
| `ACLS_JWT_AUDIENCE` | `ACLS.Api/Program.cs` | JWT audience claim. Value: `"acls-clients"` |
| `ACLS_JWT_EXPIRY_MINUTES` | `ACLS.Api/Program.cs` | Token lifetime in minutes. Default: `60` |

### 3.3 Storage

| Variable | Used in | Description |
|---|---|---|
| `ACLS_STORAGE_CONNECTION` | `ACLS.Infrastructure/Storage/AzureBlobStorageService.cs` | Azure Blob Storage connection string |
| `ACLS_STORAGE_CONTAINER` | `ACLS.Infrastructure/Storage/AzureBlobStorageService.cs` | Blob container name (e.g. `acls-media`) |

**Development value** (Azurite emulator):
```json
{
  "Storage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "MediaContainerName": "acls-media-dev"
  }
}
```

### 3.4 Notifications

| Variable | Used in | Description |
|---|---|---|
| `ACLS_NOTIFICATION_KEY` | `ACLS.Infrastructure/Notifications/` | API key for the configured notification provider |
| `ACLS_NOTIFICATION_EMAIL_FROM` | `ACLS.Infrastructure/Notifications/` | Sender email address (e.g. `noreply@acls.app`) |
| `ACLS_NOTIFICATION_SMS_FROM` | `ACLS.Infrastructure/Notifications/` | Sender phone number (E.164 format) |

### 3.5 Observability (Staging and Production only)

| Variable | Used in | Description |
|---|---|---|
| `ACLS_APPINSIGHTS_CONNECTION` | `ACLS.Api/Program.cs` | Azure Application Insights connection string |

---

## 4. `appsettings.json` Structure (Committed — No Secrets)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "acls-api",
    "Audience": "acls-clients",
    "ExpiryMinutes": 60
  },
  "Storage": {
    "ConnectionString": "",
    "MediaContainerName": "acls-media"
  },
  "Notification": {
    "ApiKey": "",
    "EmailFrom": "noreply@acls.app",
    "SmsFrom": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "ACLS": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 5. `.gitignore` Entries (Mandatory)

These files must be listed in the root `.gitignore`:

```gitignore
# Local development secrets — never commit
appsettings.Development.json
appsettings.*.json
!appsettings.json

# Android local secrets
local.properties

# iOS local secrets
*.xcconfig
!*.shared.xcconfig

# React local secrets
.env.local
.env.development.local
.env.staging.local
.env.production.local
```

---

## 6. Client Configuration by Platform

### React (`ResidentApp.Web`)

```bash
# .env.local (gitignored)
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
```

Variable is accessed via `process.env.NEXT_PUBLIC_API_URL`. Never hardcode the URL.

### Android (`ResidentApp.Android`, `StaffApp.Android`)

```properties
# local.properties (gitignored)
API_BASE_URL=http://10.0.2.2:5000/api/v1/
```

`10.0.2.2` is the Android emulator's alias for the host machine's localhost. Read in `build.gradle.kts` and injected into `BuildConfig.API_BASE_URL`.

### iOS (`ResidentApp.iOS`, `StaffApp.iOS`)

```
// Debug.xcconfig (gitignored)
API_BASE_URL = http://localhost:5000/api/v1/
```

Read from `Info.plist` at build time. Accessed via `Configuration.apiBaseURL` in `APIClient.swift`.

---

## 7. GitHub Actions Secrets

The following secrets must be configured at the repository level in GitHub Settings → Secrets:

| Secret name | Used by pipeline | Description |
|---|---|---|
| `AZURE_CREDENTIALS` | `infrastructure-ci.yml` | Azure service principal JSON for Terraform |
| `ACLS_STAGING_DB_CONNECTION` | `backend-ci.yml` (staging deploy) | Staging SQL connection string |
| `ACLS_PROD_DB_CONNECTION` | `backend-ci.yml` (prod deploy) | Production SQL connection string |
| `ACLS_STAGING_JWT_SECRET` | `backend-ci.yml` (staging deploy) | Staging JWT signing key |
| `ACLS_PROD_JWT_SECRET` | `backend-ci.yml` (prod deploy) | Production JWT signing key |
| `ACLS_STAGING_APPINSIGHTS_CONNECTION` | `backend-ci.yml` | Staging Application Insights |
| `ACLS_PROD_APPINSIGHTS_CONNECTION` | `backend-ci.yml` | Production Application Insights |

---

*End of Environment Configuration v1.0*
