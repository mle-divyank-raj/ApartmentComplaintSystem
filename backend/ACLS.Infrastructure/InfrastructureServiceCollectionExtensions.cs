using ACLS.Application.Common.Interfaces;
using ACLS.Domain.Dispatch;
using ACLS.Domain.Notifications;
using ACLS.Domain.Reporting;
using ACLS.Domain.Storage;
using ACLS.Infrastructure.Auth;
using ACLS.Infrastructure.Dispatch;
using ACLS.Infrastructure.Notifications;
using ACLS.Infrastructure.Reporting;
using ACLS.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ACLS.Infrastructure;

/// <summary>
/// Registers all ACLS.Infrastructure services in the DI container.
/// Call from ACLS.Api Program.cs: builder.Services.AddInfrastructure(config);
///
/// Note: ICurrentPropertyContext is registered in ACLS.Api (as a Scoped service backed by
/// CurrentPropertyContext populated by TenancyMiddleware) — not here.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDispatchService, DispatchService>();
        services.AddScoped<IStorageService, BlobStorageService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        // Notification channel providers — both registered so NotificationService
        // receives IEnumerable<INotificationChannel> with Email + SMS implementations.
        services.AddScoped<INotificationChannel, EmailNotificationProvider>();
        services.AddScoped<INotificationChannel, SmsNotificationProvider>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
