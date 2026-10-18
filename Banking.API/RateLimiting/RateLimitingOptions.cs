using System.ComponentModel.DataAnnotations;

namespace Banking.API.RateLimiting;

/// <summary>
/// Request limits, bound from the <c>RateLimiting</c> configuration section. Each policy is a fixed
/// window: at most <see cref="RateLimitPolicyOptions.PermitLimit"/> requests per
/// <see cref="RateLimitPolicyOptions.WindowSeconds"/> for each partition (an IP address or a customer).
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>Login and registration, counted per client IP address, to slow down credential guessing.</summary>
    [Required]
    public RateLimitPolicyOptions Authentication { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Deposits, withdrawals and interest, counted per authenticated customer.</summary>
    [Required]
    public RateLimitPolicyOptions MoneyOperations { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };
}

/// <summary>Limits of one policy.</summary>
public sealed class RateLimitPolicyOptions
{
    /// <summary>Requests allowed per window and partition.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; }

    /// <summary>Length of the window in seconds.</summary>
    [Range(1, 24 * 60 * 60)]
    public int WindowSeconds { get; init; }

    /// <summary>The window as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);
}
