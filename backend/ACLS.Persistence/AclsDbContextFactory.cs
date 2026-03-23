using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ACLS.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI tools (dotnet ef migrations add, etc.)
/// to instantiate AclsDbContext without a running application or startup project.
/// The connection string here is a placeholder; the real string is provided via
/// environment variables / appsettings at runtime.
/// </summary>
public sealed class AclsDbContextFactory : IDesignTimeDbContextFactory<AclsDbContext>
{
    public AclsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AclsDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=ACLS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true",
            sql => sql.MigrationsAssembly(typeof(AclsDbContext).Assembly.FullName));

        return new AclsDbContext(optionsBuilder.Options);
    }
}
