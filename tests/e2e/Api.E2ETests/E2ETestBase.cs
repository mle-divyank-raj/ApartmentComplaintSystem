using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ACLS.Application.Common.Interfaces;
using ACLS.Domain.Identity;
using ACLS.Domain.Notifications;
using ACLS.Domain.Storage;
using ACLS.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace Api.E2ETests;

/// <summary>
/// Base class for all E2E tests. Manages a shared SQL Server TestContainer and a
/// WebApplicationFactory whose DB connection string and JWT settings are overridden
/// to point at the container. Infrastructure services (blob storage, notifications)
/// are replaced with NSubstitute fakes so tests run without Azure credentials.
/// </summary>
public abstract class E2ETestBase
{
    private static MsSqlContainer _sqlContainer = null!;
    protected static WebApplicationFactory<Program> Factory { get; private set; } = null!;

    private const string TestJwtSecret = "SuperSecretTestKeyForE2ETesting1234567890AB";
    private const string TestJwtIssuer = "acls-api";
    private const string TestJwtAudience = "acls-clients";

    [OneTimeSetUp]
    public static async Task StartInfrastructure()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sqlContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseSetting("ConnectionStrings:DefaultConnection", _sqlContainer.GetConnectionString());
                host.UseSetting("JwtSettings:Secret", TestJwtSecret);
                host.UseSetting("JwtSettings:Issuer", TestJwtIssuer);
                host.UseSetting("JwtSettings:Audience", TestJwtAudience);

                host.ConfigureServices(services =>
                {
                    // Replace AclsDbContext with TestContainers-backed version
                    services.RemoveAll<DbContextOptions<AclsDbContext>>();
                    services.RemoveAll<AclsDbContext>();
                    services.AddDbContext<AclsDbContext>(options =>
                        options.UseSqlServer(_sqlContainer.GetConnectionString()));

                    // Replace blob storage with a no-op fake — no Azure credentials needed
                    services.RemoveAll<IStorageService>();
                    var fakeStorage = Substitute.For<IStorageService>();
                    fakeStorage.UploadAsync(default!, default!, default!, default)
                        .ReturnsForAnyArgs("https://fake.blob.test/file.jpg");
                    services.AddScoped(_ => fakeStorage);

                    // Replace notification service with a no-op fake
                    services.RemoveAll<INotificationService>();
                    var fakeNotifications = Substitute.For<INotificationService>();
                    services.AddScoped(_ => fakeNotifications);
                });
            });

        // Apply migrations once
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public static async Task TearDownInfrastructure()
    {
        Factory?.Dispose();
        await _sqlContainer.StopAsync();
    }

    [TearDown]
    public async Task ResetDatabaseBetweenTests()
    {
        // Truncate all data tables between tests using the test container connection
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        // Disable and re-enable FK constraints to allow truncation in dependency order
        await db.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM WorkNotes;
            DELETE FROM Media;
            DELETE FROM AuditLog;
            DELETE FROM Complaints;
            DELETE FROM Residents;
            DELETE FROM StaffMembers;
            DELETE FROM InvitationTokens;
            DELETE FROM Users;
            DELETE FROM Outages;
            DELETE FROM Units;
            DELETE FROM Buildings;
            DELETE FROM Properties;");
        await db.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
    }

    /// <summary>
    /// Creates an authenticated HTTP client carrying a JWT for the specified user.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(int userId, int propertyId, Role role)
    {
        var token = GenerateJwt(userId, propertyId, role);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated HTTP client.
    /// </summary>
    protected HttpClient CreateAnonymousClient() => Factory.CreateClient();

    /// <summary>
    /// Generates a signed JWT for the given actor, using the test secret.
    /// </summary>
    protected static string GenerateJwt(int userId, int propertyId, Role role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("role", role.ToString()),
            new Claim("property_id", propertyId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"user{userId}@test.com")
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Seeds the minimal data hierarchy needed for complaint tests.
    /// Returns (propertyId, unitId, residentUserId, residentId, staffUserId, staffMemberId).
    /// </summary>
    protected async Task<(int propertyId, int unitId, int residentUserId, int residentId, int staffUserId, int staffMemberId, int managerUserId)>
        SeedComplaintPrerequisitesAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        var property = ACLS.Domain.Properties.Property.Create("E2E Test Property", "1 E2E Street");
        await db.Properties.AddAsync(property);
        await db.SaveChangesAsync();

        var building = ACLS.Domain.Properties.Building.Create(property.PropertyId, "E2E Block");
        await db.Buildings.AddAsync(building);
        await db.SaveChangesAsync();

        var unit = ACLS.Domain.Properties.Unit.Create(building.BuildingId, "E01", 1);
        await db.Units.AddAsync(unit);
        await db.SaveChangesAsync();

        var residentUser = User.Create("e2e.resident@test.com", "$2a$11$hash", "E2E", "Resident", Role.Resident, property.PropertyId);
        var staffUser = User.Create("e2e.staff@test.com", "$2a$11$hash", "E2E", "Staff", Role.MaintenanceStaff, property.PropertyId);
        var managerUser = User.Create("e2e.manager@test.com", "$2a$11$hash", "E2E", "Manager", Role.Manager, property.PropertyId);
        await db.Users.AddRangeAsync(residentUser, staffUser, managerUser);
        await db.SaveChangesAsync();

        var resident = ACLS.Domain.Residents.Resident.Create(residentUser.UserId, unit.UnitId);
        await db.Residents.AddAsync(resident);
        await db.SaveChangesAsync();

        var staff = ACLS.Domain.Staff.StaffMember.Create(staffUser.UserId, "Plumber", ["Plumbing"]);
        await db.StaffMembers.AddAsync(staff);
        await db.SaveChangesAsync();

        return (property.PropertyId, unit.UnitId, residentUser.UserId, resident.ResidentId, staffUser.UserId, staff.StaffMemberId, managerUser.UserId);
    }
}
