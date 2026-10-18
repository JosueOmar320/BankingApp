using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Idempotency;

/// <summary>
/// Marks a money operation as accepting the optional <c>Idempotency-Key</c> header, handled by
/// <see cref="IdempotencyFilter"/>: a repeated key with the same request replays the first
/// response instead of applying the operation again.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : ServiceFilterAttribute
{
    /// <summary>Resolves <see cref="IdempotencyFilter"/> from the request's services.</summary>
    public IdempotentAttribute()
        : base(typeof(IdempotencyFilter))
    {
    }
}
