# Observability

**Document:** `docs/07_Implementation/observability.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> This document defines the logging, metrics, tracing, and health check standards for ACLS. All backend code generated must follow these conventions. No session may introduce a new logging pattern, metric name, or health check without referencing this document first.

---

## 1. Observability Stack

| Signal | Tooling | Sink |
|---|---|---|
| Structured logs | `Microsoft.Extensions.Logging` + OpenTelemetry log bridge | Azure Monitor / Application Insights |
| Distributed traces | OpenTelemetry SDK + ASP.NET Core instrumentation | Application Insights (Transaction search) |
| Metrics | OTel Metrics API + ASP.NET Core built-ins | Azure Monitor Metrics |
| Health checks | `Microsoft.Extensions.Diagnostics.HealthChecks` | `/healthz` endpoint |

**Local development:** Console exporter for logs and traces. No Azure Monitor connection required.  
**Staging and production:** Azure Monitor / Application Insights via `Azure.Monitor.OpenTelemetry.AspNetCore`.

---

## 2. Required NuGet Packages

```xml
<!-- In ACLS.Api.csproj -->
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.*" />
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.*" />
```

---

## 3. OpenTelemetry Configuration

Configured once in `ACLS.Api/Program.cs`. Connection strings injected from environment variables — never hardcoded.

```csharp
// In Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// Azure Monitor only in non-development environments
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = builder.Configuration["ACLS_APPINSIGHTS_CONNECTION"];
        });
}
```

---

## 4. Structured Logging Standards

### 4.1 Log Levels

| Level | When to use |
|---|---|
| `Trace` | Not used in production. Development debugging only. |
| `Debug` | Detailed flow information useful during development. Disabled in staging/prod. |
| `Information` | Normal operation milestones: request received, command handled, event published. |
| `Warning` | Unexpected but recoverable: validation failure from untrusted input, slow query (>500ms), null from repository where a value was expected. |
| `Error` | Operation failed: unhandled exception, infrastructure failure, transaction rollback. |
| `Critical` | System cannot function: database unreachable, Key Vault unreachable, startup failure. |

### 4.2 Structured Log Message Conventions

Use `ILogger<T>` with structured (named) parameters. Never string interpolation in log messages.

```csharp
// CORRECT — structured parameters
_logger.LogInformation(
    "Complaint {ComplaintId} assigned to staff {StaffMemberId} in property {PropertyId}",
    complaintId, staffMemberId, propertyId);

// WRONG — string interpolation loses structured query capability
_logger.LogInformation(
    $"Complaint {complaintId} assigned to staff {staffMemberId}");
```

### 4.3 Standard Log Fields

Every log message in a request context automatically includes these fields via the OTel SDK:
- `TraceId` — distributed trace ID
- `SpanId` — current span ID
- `RequestPath` — HTTP path
- `RequestMethod` — HTTP verb
- `StatusCode` — HTTP response code

Application code adds:
- `PropertyId` — always logged when available, for multi-tenancy filtering in Azure Monitor
- `UserId` — always logged when available
- `ComplaintId` — logged in all complaint-related operations
- `Duration` — logged for operations with SLA implications

### 4.4 What Must Never Be Logged

The following data must never appear in any log entry at any level:

| Data | Why |
|---|---|
| Passwords or password hashes | PII + security |
| JWT tokens (full or partial) | Security — leaks authentication material |
| `PasswordHash` column values | Security |
| Full request bodies on `POST /auth/login` | Contains password |
| Resident names or email addresses in error logs | PII |
| Credit card or payment data | Not applicable in V1 — precautionary |

If a log message would reveal any of the above, log the entity ID instead.

### 4.5 Logging in Command Handlers

`LoggingBehaviour` (MediatR pipeline) logs the start and completion of every command and query automatically. Individual handlers do not need to log start/end — they log only exceptional or significant domain events.

```csharp
// LoggingBehaviour logs automatically:
// "Handling AssignComplaintCommand {PropertyId: 1, ComplaintId: 42}"
// "Handled AssignComplaintCommand in 45ms — Success"

// Handler adds only domain-significant events:
_logger.LogInformation(
    "Complaint {ComplaintId} SOS triggered in property {PropertyId}. " +
    "Notifying {StaffCount} on-call staff.",
    complaint.ComplaintId, propertyId, staffCount);
```

---

## 5. Health Checks

### 5.1 Endpoint

```
GET /healthz
```

Returns `200 OK` with a JSON body when all dependencies are healthy. Returns `503 Service Unavailable` when any critical dependency is unhealthy.

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration["ACLS_DB_CONNECTION"],
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql"])
    .AddAzureBlobStorage(
        connectionString: builder.Configuration["ACLS_STORAGE_CONNECTION"],
        containerName: builder.Configuration["Storage:MediaContainerName"],
        name: "blob-storage",
        failureStatus: HealthStatus.Degraded,
        tags: ["storage"]);

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 5.2 Health Check Response Shape

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.012",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.008",
      "tags": ["db", "sql"]
    },
    "blob-storage": {
      "status": "Healthy",
      "duration": "00:00:00.004",
      "tags": ["storage"]
    }
  }
}
```

`Blob-storage` failure status is `Degraded` (not `Unhealthy`) because the API can still serve read requests without storage. A `Degraded` status returns HTTP 200. An `Unhealthy` status returns HTTP 503.

---

## 6. Performance Targets and Alerting Thresholds

From `docs/03_Architecture/system_overview.md` and the project NFRs:

| Metric | Target | Alert threshold | NFR |
|---|---|---|---|
| API response time (p95) | ≤ 2 seconds | > 2 seconds for 5 consecutive minutes | NFR-02 |
| Outage broadcast duration | ≤ 60 seconds for 500 messages | > 90 seconds | NFR-12 |
| System availability | ≥ 99% | < 99% over 24 hours | NFR-01 |
| Database query time (p95) | ≤ 500ms | > 500ms logged as Warning | NFR-02 |
| Error rate | < 1% of requests | > 1% over 5 minutes | NFR-01 |

---

## 7. Local Development Observability

In local development (`ASPNETCORE_ENVIRONMENT=Development`):
- Logs output to the console in structured JSON format
- No Azure Monitor connection required
- OTel traces output to the console exporter
- Swagger UI available at `http://localhost:5000/swagger`
- Health check available at `http://localhost:5000/healthz`

```json
// appsettings.Development.json — log level overrides
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "ACLS": "Debug"
    }
  }
}
```

```json
// appsettings.json — production defaults
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "ACLS": "Information"
    }
  }
}
```

---

*End of Observability v1.0*
