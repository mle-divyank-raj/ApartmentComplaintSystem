using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ACLS.Application.Common.Interfaces;
using ACLS.Domain.AuditLog;
using ACLS.Domain.Complaints;
using ACLS.Domain.Outages;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;
using ACLS.Domain.Identity;
using ACLS.Persistence.Repositories;

namespace ACLS.Persistence;

/// <summary>
/// DI registration extension for all ACLS.Persistence services.
/// Called from ACLS.Api Program.cs: builder.Services.AddPersistence(configuration);
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AclsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AclsDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AclsDbContext>());

        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IResidentRepository, ResidentRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IOutageRepository, OutageRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
