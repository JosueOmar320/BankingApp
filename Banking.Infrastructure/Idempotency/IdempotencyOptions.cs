using System.ComponentModel.DataAnnotations;

namespace Banking.Infrastructure.Idempotency;

/// <summary>Settings of the <c>Idempotency-Key</c> support (the <c>Idempotency</c> configuration section).</summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>How long a key and its stored response are kept before the cleanup removes them.</summary>
    [Range(1, 24 * 30)]
    public int KeyLifetimeHours { get; set; } = 24;

    /// <summary>How often the cleanup runs after the first run at start-up.</summary>
    [Range(1, 24 * 60)]
    public int CleanupIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// How long a reservation without a response is trusted. A request killed between reserving
    /// the key and storing its response leaves such a row; after this time a new request with the
    /// same key takes it over instead of being told the first one is still running.
    /// </summary>
    [Range(5, 3600)]
    public int PendingTimeoutSeconds { get; set; } = 60;

    public TimeSpan KeyLifetime => TimeSpan.FromHours(KeyLifetimeHours);

    public TimeSpan CleanupInterval => TimeSpan.FromMinutes(CleanupIntervalMinutes);

    public TimeSpan PendingTimeout => TimeSpan.FromSeconds(PendingTimeoutSeconds);
}
