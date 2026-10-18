using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Banking.Infrastructure.Idempotency;

/// <summary>
/// Purges idempotency keys older than <see cref="IdempotencyOptions.KeyLifetime"/>: once when the
/// host starts (the schema has been migrated by then) and then every
/// <see cref="IdempotencyOptions.CleanupInterval"/>. A failed run is logged and retried at the next tick.
/// </summary>
public sealed partial class IdempotencyKeyCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<IdempotencyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdempotencyKeyCleanupService> _logger;

    public IdempotencyKeyCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<IdempotencyOptions> options,
        TimeProvider timeProvider,
        ILogger<IdempotencyKeyCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanUpOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(_options.Value.CleanupInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host shutting down.
            }
        }
    }

    /// <summary>Runs one purge; never throws, so a transient database error does not stop the service.</summary>
    internal async Task<int> CleanUpOnceAsync(CancellationToken cancellationToken)
    {
        var threshold = _timeProvider.GetUtcNow().UtcDateTime - _options.Value.KeyLifetime;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            var removed = await store.RemoveOlderThanAsync(threshold, cancellationToken);
            if (removed > 0)
            {
                LogRemoved(_logger, removed, threshold);
            }

            return removed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailed(_logger, ex);
            return 0;
        }
    }

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Removed {Count} idempotency key(s) first seen before {Threshold:O}")]
    private static partial void LogRemoved(ILogger logger, int count, DateTime threshold);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning, Message = "Idempotency key cleanup failed; it will run again at the next interval")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
