using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Banking.Infrastructure.Persistence;

/// <summary>Applies pending migrations (and optionally seeds sample data) at startup.</summary>
public static partial class DatabaseInitializer
{
    /// <summary>
    /// EF Core takes an exclusive lock (a row in <c>__EFMigrationsLock</c>) while it applies
    /// migrations and waits indefinitely when the row already exists. A process killed in the
    /// middle of a migration (for example stopped from the IDE) leaves that row behind and every
    /// later start hangs before Kestrel listens. Migrations of this schema take milliseconds, so a
    /// lock older than this cannot belong to a live migration and is removed before migrating.
    /// </summary>
    internal static readonly TimeSpan StaleMigrationLockAge = TimeSpan.FromMinutes(2);

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seedSampleData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            await RemoveStaleMigrationLockAsync(context, timeProvider, logger, cancellationToken);

            var names = string.Join(", ", pending);
            LogApplyingMigrations(logger, pending.Count, names);
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (seedSampleData)
        {
            await DevelopmentDataSeeder.SeedAsync(context, timeProvider, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes migration lock rows older than <see cref="StaleMigrationLockAge"/> (or whose
    /// timestamp cannot be read). Returns the number of rows removed.
    /// </summary>
    internal static async Task<int> RemoveStaleMigrationLockAsync(BankingDbContext context, TimeProvider timeProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using (var tableExists = connection.CreateCommand())
            {
                tableExists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsLock'";
                if (Convert.ToInt64(await tableExists.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 0)
                {
                    return 0;
                }
            }

            var staleIds = new List<long>();
            var threshold = timeProvider.GetUtcNow() - StaleMigrationLockAge;

            await using (var readLocks = connection.CreateCommand())
            {
                readLocks.CommandText = "SELECT \"Id\", \"Timestamp\" FROM \"__EFMigrationsLock\"";
                await using var reader = await readLocks.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var id = reader.GetInt64(0);
                    var raw = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString();
                    var readable = DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var takenAt);

                    if (!readable || takenAt <= threshold)
                    {
                        staleIds.Add(id);
                        LogStaleMigrationLock(logger, id, raw ?? "<null>");
                    }
                }
            }

            foreach (var id in staleIds)
            {
                await using var delete = connection.CreateCommand();
                delete.CommandText = "DELETE FROM \"__EFMigrationsLock\" WHERE \"Id\" = $id";
                var parameter = delete.CreateParameter();
                parameter.ParameterName = "$id";
                parameter.Value = id;
                delete.Parameters.Add(parameter);
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            return staleIds.Count;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Applying {Count} pending migration(s): {Migrations}")]
    private static partial void LogApplyingMigrations(ILogger logger, int count, string migrations);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Removing stale migration lock {LockId} taken at {TakenAt}; a previous run was interrupted while migrating")]
    private static partial void LogStaleMigrationLock(ILogger logger, long lockId, string takenAt);
}
