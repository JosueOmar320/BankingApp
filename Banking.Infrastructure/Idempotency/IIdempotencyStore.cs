namespace Banking.Infrastructure.Idempotency;

/// <summary>
/// Persistence of <c>Idempotency-Key</c> uses, partitioned by customer. The API reserves a key
/// before running a money operation, completes it with the response or releases it on failure,
/// and a background service purges old keys.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Reserves <paramref name="key"/> for <paramref name="customerId"/>. Returns <see langword="null"/>
    /// when the reservation succeeded (the caller owns the key and must complete or release it), or
    /// the existing record when the customer already used the key: completed, with the response to
    /// replay, or still pending.
    /// </summary>
    Task<IdempotencyKey?> TryReserveAsync(int customerId, string key, string requestHash, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Stores the response of the request that reserved the key.</summary>
    Task CompleteAsync(int customerId, string key, int statusCode, string contentType, string body, CancellationToken cancellationToken = default);

    /// <summary>Deletes the record so the key can be used again. Returns <see langword="false"/> when there was none.</summary>
    Task<bool> ReleaseAsync(int customerId, string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes every key first seen before <paramref name="thresholdUtc"/>. Returns the number removed.</summary>
    Task<int> RemoveOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);
}
