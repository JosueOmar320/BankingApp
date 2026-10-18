using Banking.Infrastructure.Persistence;
using Banking.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Banking.Tests.Infrastructure;

/// <summary>
/// Startup must survive a <c>__EFMigrationsLock</c> row left behind by a process that was killed
/// while migrating; otherwise EF Core waits for it forever and the API never listens.
/// </summary>
public class DatabaseInitializerTests
{
    private const string CreateLockTable =
        "CREATE TABLE \"__EFMigrationsLock\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK___EFMigrationsLock\" PRIMARY KEY, \"Timestamp\" TEXT NOT NULL)";

    private string _databasePath = string.Empty;

    [SetUp]
    public void CreateDatabaseFile()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"banking-init-{Guid.NewGuid():N}.db");
    }

    [TearDown]
    public void DeleteDatabaseFile()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            File.Delete(_databasePath + suffix);
        }
    }

    [Test]
    [Timeout(30_000)]
    public async Task InitializeDatabase_RemovesStaleLockLeftByInterruptedRun_AndMigrates()
    {
        var now = FixedTimeProvider.DefaultNow;
        InsertLockRow(takenAt: now - DatabaseInitializer.StaleMigrationLockAge - TimeSpan.FromSeconds(1));
        await using var services = BuildServices(now);

        await services.InitializeDatabaseAsync(seedSampleData: false);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        Assert.Multiple(async () =>
        {
            Assert.That(await context.Database.GetPendingMigrationsAsync(), Is.Empty);
            Assert.That(CountLockRows(), Is.Zero, "the stale lock must be gone once the migration finished");
        });
    }

    [Test]
    public async Task RemoveStaleMigrationLock_KeepsALockThatMayStillBeInUse()
    {
        var now = FixedTimeProvider.DefaultNow;
        InsertLockRow(takenAt: now - TimeSpan.FromSeconds(10));
        await using var services = BuildServices(now);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        var removed = await DatabaseInitializer.RemoveStaleMigrationLockAsync(context, new FixedTimeProvider(now), NullLogger.Instance, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.Zero);
            Assert.That(CountLockRows(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RemoveStaleMigrationLock_DoesNothing_WhenTheLockTableDoesNotExist()
    {
        await using var services = BuildServices(FixedTimeProvider.DefaultNow);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        var removed = await DatabaseInitializer.RemoveStaleMigrationLockAsync(context, new FixedTimeProvider(), NullLogger.Instance, CancellationToken.None);

        Assert.That(removed, Is.Zero);
    }

    private ServiceProvider BuildServices(DateTimeOffset now)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(now));
        services.AddDbContext<BankingDbContext>(options => options.UseSqlite($"Data Source={_databasePath}"));
        return services.BuildServiceProvider();
    }

    private void InsertLockRow(DateTimeOffset takenAt)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = CreateLockTable + "; INSERT INTO \"__EFMigrationsLock\" (\"Id\", \"Timestamp\") VALUES (1, $takenAt)";
        command.Parameters.AddWithValue("$takenAt", takenAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private long CountLockRows()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsLock\"";
        return (long)command.ExecuteScalar()!;
    }
}
