using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Idempotency;

/// <summary>
/// <see cref="IIdempotencyStore"/> on the banking database. Every call uses its own short-lived
/// <see cref="BankingDbContext"/>: the key must be committed independently of the money operation's
/// unit of work, and a failed operation must not drag the key's row into its retry.
/// </summary>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly DbContextOptions<BankingDbContext> _options;

    public IdempotencyStore(DbContextOptions<BankingDbContext> options)
    {
        _options = options;
    }

    public async Task<IdempotencyKey?> TryReserveAsync(int customerId, string key, string requestHash, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var context = new BankingDbContext(_options);

        // The common replay finds the row without attempting an insert (and without EF logging a failed command).
        var existing = await FindAsync(context, customerId, key, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        context.IdempotencyKeys.Add(new IdempotencyKey(customerId, key, requestHash, nowUtc));
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException ex) when (SqliteErrors.IsUniqueConstraintViolation(ex))
        {
            // Lost the race against a concurrent request with the same key: the primary key decided who owns it.
            context.ChangeTracker.Clear();
            return await FindAsync(context, customerId, key, cancellationToken);
        }
    }

    private static Task<IdempotencyKey?> FindAsync(BankingDbContext context, int customerId, string key, CancellationToken cancellationToken)
        => context.IdempotencyKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.CustomerId == customerId && k.Key == key, cancellationToken);

    public async Task CompleteAsync(int customerId, string key, int statusCode, string contentType, string body, CancellationToken cancellationToken = default)
    {
        await using var context = new BankingDbContext(_options);

        var record = await context.IdempotencyKeys.SingleAsync(k => k.CustomerId == customerId && k.Key == key, cancellationToken);
        record.Complete(statusCode, contentType, body);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ReleaseAsync(int customerId, string key, CancellationToken cancellationToken = default)
    {
        await using var context = new BankingDbContext(_options);

        var removed = await context.IdempotencyKeys
            .Where(k => k.CustomerId == customerId && k.Key == key)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0;
    }

    public async Task<int> RemoveOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default)
    {
        await using var context = new BankingDbContext(_options);

        return await context.IdempotencyKeys
            .Where(k => k.CreatedAt < thresholdUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
