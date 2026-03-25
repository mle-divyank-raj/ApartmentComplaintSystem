# Start the Background Worker

**Project:** `backend/ACLS.Worker/ACLS.Worker.csproj`  
**Framework:** .NET 8 Worker Service  
**Role:** Processes domain events asynchronously and runs scheduled background jobs.

---

## What the Worker Does

| Component | Description |
|---|---|
| `ComplaintAssignedEventHandler` | Sends assignment notification to the resident |
| `ComplaintResolvedEventHandler` | Sends resolution notification and triggers TAT calculation |
| `FeedbackSubmittedEventHandler` | Updates staff average rating |
| `OutageDeclaredEventHandler` | Triggers broadcast to all affected residents |
| `SosTriggeredEventHandler` | Concurrently notifies all on-call staff via `Task.WhenAll` |
| `CalculateTatJob` | Calculates `Complaint.Tat` (time-to-resolve in minutes) |
| `UpdateAverageRatingJob` | Recalculates `StaffMember.AverageRating` from resolved complaints |
| `BroadcastOutageNotificationJob` | Fan-out notification broadcast (target: 500 messages/60 s) |

---

## Prerequisites

- .NET 8 SDK installed — `dotnet --version`
- SQL Server running — see [Local Dev Environment](localDevEnvironment.md)
- The Backend API should also be running if you need the full system — see [Start the Backend](startBackend.md)

---

## Step 1 — Configure Secrets

The Worker uses .NET User Secrets (secrets ID: `acls-worker-secrets`):

```bash
cd backend/ACLS.Worker

dotnet user-secrets set "ACLS_DB_CONNECTION" "Server=localhost,1433;Database=AclsDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
```

For notification services (email/SMS), add these if you have provider credentials:

```bash
dotnet user-secrets set "ACLS_NOTIFICATION_KEY"         "your-provider-api-key"
dotnet user-secrets set "ACLS_NOTIFICATION_EMAIL_FROM"  "noreply@acls.app"
dotnet user-secrets set "ACLS_NOTIFICATION_SMS_FROM"    "+15550000000"
```

> In local development, if notification credentials are not set, the worker will still run
> but will skip actual email/SMS delivery. No crash occurs.

---

## Step 2 — Restore and Run

```bash
cd backend/ACLS.Worker

dotnet restore
dotnet run
```

To run in watch mode:

```bash
dotnet watch run
```

---

## Step 3 — Verify

The Worker does not expose an HTTP port. Confirm it is running by checking the console output:

```
info: ACLS.Worker[0]
      Worker host started.
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Structured logs are written to stdout following the pattern in
`docs/07_Implementation/observability.md`.

---

## Configuration Keys

| Key | Required | Notes |
|---|---|---|
| `ACLS_DB_CONNECTION` | Yes | SQL Server connection string |
| `ACLS_NOTIFICATION_KEY` | No* | Provider API key — dev runs without it |
| `ACLS_NOTIFICATION_EMAIL_FROM` | No* | Sender email address |
| `ACLS_NOTIFICATION_SMS_FROM` | No* | E.164 sender phone number |
| `ACLS_APPINSIGHTS_CONNECTION` | No | Staging/Production only |
| `ASPNETCORE_ENVIRONMENT` | No | Set to `Development` for verbose logs |

*Required in Staging and Production.
