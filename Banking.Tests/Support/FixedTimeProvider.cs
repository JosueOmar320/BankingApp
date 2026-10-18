namespace Banking.Tests.Support;

/// <summary>A clock frozen at a known instant so timestamps are deterministic.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset? now = null)
    {
        _now = now ?? DefaultNow;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
