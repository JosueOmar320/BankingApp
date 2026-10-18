using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Banking.Infrastructure.Persistence;

/// <summary>Applies pending migrations (and optionally seeds sample data) at startup.</summary>
public static partial class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seedSampleData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            var names = string.Join(", ", pending);
            LogApplyingMigrations(logger, pending.Count, names);
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (seedSampleData)
        {
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            await DevelopmentDataSeeder.SeedAsync(context, timeProvider, cancellationToken);
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Applying {Count} pending migration(s): {Migrations}")]
    private static partial void LogApplyingMigrations(ILogger logger, int count, string migrations);
}
