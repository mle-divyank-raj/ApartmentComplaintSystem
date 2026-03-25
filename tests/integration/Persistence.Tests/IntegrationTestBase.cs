using Microsoft.EntityFrameworkCore;
using ACLS.Persistence;
using Respawn;
using Testcontainers.MsSql;
using NUnit.Framework;

namespace Persistence.Tests;

/// <summary>
/// Base class for all integration tests that require a real SQL Server database.
/// One container is shared across all tests in the session (OneTimeSetUp/TearDown).
/// Respawn resets data between each individual test (TearDown).
/// </summary>
[TestFixture]
public abstract class IntegrationTestBase
{
    private static MsSqlContainer _sqlContainer = null!;
    protected AclsDbContext Context { get; private set; } = null!;
    private Respawner _respawner = null!;

    [OneTimeSetUp]
    public static async Task StartContainer()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await _sqlContainer.StartAsync();
    }

    [OneTimeTearDown]
    public static async Task StopContainer()
        => await _sqlContainer.StopAsync();

    [SetUp]
    public async Task SetUpContext()
    {
        var options = new DbContextOptionsBuilder<AclsDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        Context = new AclsDbContext(options);
        await Context.Database.MigrateAsync();

        _respawner = await Respawner.CreateAsync(
            _sqlContainer.GetConnectionString(),
            new RespawnerOptions
            {
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
    }

    [TearDown]
    public async Task ResetDatabase()
    {
        await _respawner.ResetAsync(_sqlContainer.GetConnectionString());
        await Context.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh DbContext using the same container connection string.
    /// Use this to verify state after an operation without tracking interference.
    /// Caller is responsible for disposing the returned context.
    /// </summary>
    protected AclsDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<AclsDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;
        return new AclsDbContext(options);
    }
}
