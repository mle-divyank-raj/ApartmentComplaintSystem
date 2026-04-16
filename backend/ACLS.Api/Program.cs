using System.Text;
using ACLS.Api.Middleware;
using ACLS.Api.Services;
using ACLS.Application;
using ACLS.Application.Common.Interfaces;
using ACLS.Infrastructure;
using ACLS.Persistence;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Storage.Blobs;
using HealthChecks.Azure.Storage.Blobs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ── Service Registration (order follows Clean Architecture: outer layers last) ──

// Data Access
builder.Services.AddPersistence(config);

// Infrastructure (auth, storage, reporting, dispatch)
builder.Services.AddInfrastructure();

// Application (MediatR, validators, pipeline behaviours)
builder.Services.AddApplication();

// Current request tenant context — scoped so it spans the entire HTTP request
builder.Services.AddScoped<CurrentPropertyContext>();
builder.Services.AddScoped<ICurrentPropertyContext>(sp => sp.GetRequiredService<CurrentPropertyContext>());

// ── JWT Bearer Authentication ────────────────────────────────────────────────

var jwtSecret = config["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is required but not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["JwtSettings:Issuer"] ?? "acls-api",

            ValidateAudience = true,
            ValidAudience = config["JwtSettings:Audience"] ?? "acls-clients",

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // Strict expiry enforcement

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // Propagate 401 errors back to client cleanly (no redirect)
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    "{\"errorCode\":\"Auth.TokenInvalid\",\"detail\":\"Access token is missing or invalid.\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────────────

var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ── MVC / Controllers ────────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger / OpenAPI ────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Apartment Complaint & Lifecycle System API",
        Version = "v1",
        Description = "REST API for the ACLS multi-tenant property management platform."
    });

    // Bearer token support in Swagger UI
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ── OpenTelemetry ────────────────────────────────────────────────────────────

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
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

// ── Health Checks ────────────────────────────────────────────────────────────

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration["ACLS_DB_CONNECTION"]!,
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql"])
    .AddAzureBlobStorage(
        clientFactory: _ => new BlobServiceClient(builder.Configuration["ACLS_STORAGE_CONNECTION"]!),
        optionsFactory: _ => new AzureBlobStorageHealthCheckOptions
        {
            ContainerName = builder.Configuration["Storage:MediaContainerName"]
        },
        name: "blob-storage",
        failureStatus: HealthStatus.Degraded,
        tags: ["storage"]);

// ── Build ────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Middleware Pipeline (ORDER IS MANDATORY) ─────────────────────────────────
// 1. Exception handler must be FIRST so it catches errors from all subsequent middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. CORS — must be before authentication/authorization
app.UseCors("AllowFrontend");

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 3. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ACLS API v1"));
}

// 4. Authentication (validates JWT, populates User.Claims) — MUST precede TenancyMiddleware
app.UseAuthentication();

// 5. Authorization (evaluates [Authorize] policies) — MUST follow UseAuthentication
app.UseAuthorization();

// 6. Tenancy middleware — reads validated JWT claims, populates ICurrentPropertyContext
//    Must run AFTER UseAuthentication so HttpContext.User.Claims are available
app.UseMiddleware<TenancyMiddleware>();

// 7. Map controller routes
app.MapControllers();

// 8. Health checks endpoint
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

await app.RunAsync();

// Exposed for integration test WebApplicationFactory<Program> bootstrapping
public partial class Program { }
