using System.Reflection;
using ACLS.Application.Common.Behaviours;
using ACLS.Infrastructure;
using ACLS.Persistence;
using ACLS.Worker.Jobs;
using Azure.Monitor.OpenTelemetry.Exporter;
using FluentValidation;
using MediatR;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ─────────────────────────────────────────────────────────────────────────────
// ACLS.Worker — standalone .NET Worker Service
//
// Hosts MediatR INotificationHandler<T> event consumers and background jobs.
// Receives domain events in one of two modes:
//   a) Co-hosted: API's Program.cs also scans this assembly for notification handlers.
//   b) Standalone: events are dispatched via a message bus (phase 7+).
//
// All environment variable keys follow environment_config.md Section 3.
// ─────────────────────────────────────────────────────────────────────────────

var builder = Host.CreateApplicationBuilder(args);

// ── Persistence (EF Core DbContext + all repository implementations) ──────────
builder.Services.AddPersistence(builder.Configuration);

// ── Infrastructure (NotificationService, DispatchService, etc.) ───────────────
builder.Services.AddInfrastructure();

// ── MediatR ───────────────────────────────────────────────────────────────────
// Scan both the Application assembly (command/query handlers + pipeline behaviors)
// and the Worker assembly (INotificationHandler<TDomainEvent> event handlers).
var applicationAssembly = Assembly.Load("ACLS.Application");

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        applicationAssembly,                // Application command/query handlers
        typeof(Program).Assembly);          // Worker domain event handlers

    // Application pipeline (Logging → Validation → Transaction)
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>));
});

builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// ── Worker background jobs (scoped to match repository lifetimes) ─────────────
builder.Services.AddScoped<CalculateTatJob>();
builder.Services.AddScoped<UpdateAverageRatingJob>();
builder.Services.AddScoped<BroadcastOutageNotificationJob>();

// ── OpenTelemetry (observability.md Section 3) ────────────────────────────────
const string ServiceName = "ACLS.Worker";

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(tracing =>
    {
        tracing.AddSource(ServiceName);

        if (builder.Environment.IsDevelopment())
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddRuntimeInstrumentation();

        if (builder.Environment.IsDevelopment())
            metrics.AddConsoleExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName));

    if (builder.Environment.IsDevelopment())
    {
        logging.AddConsoleExporter();
    }
});

// Azure Monitor exporter for Staging / Production (observability.md Section 3).
// Activated when ACLS_APPINSIGHTS_CONNECTION is present in the environment.
var appInsightsConnection = builder.Configuration["ACLS_APPINSIGHTS_CONNECTION"];
if (!string.IsNullOrEmpty(appInsightsConnection) && !builder.Environment.IsDevelopment())
{
    builder.Services
        .AddOpenTelemetry()
        .WithTracing(t => t.AddAzureMonitorTraceExporter(o => o.ConnectionString = appInsightsConnection))
        .WithMetrics(m => m.AddAzureMonitorMetricExporter(o => o.ConnectionString = appInsightsConnection));

    builder.Logging.AddOpenTelemetry(logging =>
        logging.AddAzureMonitorLogExporter(o => o.ConnectionString = appInsightsConnection));
}

// ─────────────────────────────────────────────────────────────────────────────
var host = builder.Build();
host.Run();
