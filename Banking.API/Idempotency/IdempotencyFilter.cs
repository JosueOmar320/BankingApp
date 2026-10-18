using System.Security.Cryptography;
using System.Text;
using Banking.Application.Interfaces;
using Banking.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Banking.API.Idempotency;

/// <summary>
/// Makes a money operation safe to retry. When the request carries an <c>Idempotency-Key</c>:
/// <list type="number">
/// <item>the key is reserved for the authenticated customer together with a fingerprint of the
/// request (method, route and body), so a concurrent duplicate is told the first request is still
/// running (<c>409</c>, code <c>IdempotencyKeyInProgress</c>);</item>
/// <item>a key already used with a different fingerprint is rejected (<c>422</c>, code
/// <c>IdempotencyKeyReused</c>);</item>
/// <item>a key already completed replays the stored status and body without touching the account
/// (<c>Idempotent-Replayed: true</c>);</item>
/// <item>otherwise the operation runs; a <c>2xx</c> response is stored under the key, while an error
/// response or exception releases the key so the same key can be retried once the cause is fixed.</item>
/// </list>
/// Requests without the header behave as before. Keys of different customers never collide.
/// </summary>
public sealed partial class IdempotencyFilter : IAsyncResourceFilter
{
    /// <summary>Request header that carries the key.</summary>
    public const string HeaderName = "Idempotency-Key";

    /// <summary>Response header set to <c>true</c> when the body is a replay of the first response.</summary>
    public const string ReplayedHeaderName = "Idempotent-Replayed";

    private const string DefaultContentType = "application/json; charset=utf-8";

    private readonly IIdempotencyStore _store;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly IOptions<IdempotencyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdempotencyFilter> _logger;

    /// <summary>Creates the filter; it is resolved per request through <see cref="IdempotentAttribute"/>.</summary>
    public IdempotencyFilter(
        IIdempotencyStore store,
        ICurrentCustomer currentCustomer,
        IOptions<IdempotencyOptions> options,
        TimeProvider timeProvider,
        ILogger<IdempotencyFilter> logger)
    {
        _store = store;
        _currentCustomer = currentCustomer;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Whether a key is acceptable: 1 to <see cref="IdempotencyKey.MaxKeyLength"/> visible ASCII characters (no spaces or control characters).</summary>
    public static bool IsWellFormed(string? key)
        => !string.IsNullOrEmpty(key)
            && key.Length <= IdempotencyKey.MaxKeyLength
            && key.All(character => character is >= '\x21' and <= '\x7E');

    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;
        var key = httpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(key))
        {
            await next();
            return;
        }

        if (!IsWellFormed(key))
        {
            throw IdempotencyKeyException.Invalid();
        }

        // Authorization already rejected anonymous callers; without a customer there is nothing to partition by.
        if (_currentCustomer.CustomerId is not { } customerId)
        {
            await next();
            return;
        }

        var fingerprint = await FingerprintAsync(httpContext.Request, httpContext.RequestAborted);
        var existing = await ReserveAsync(customerId, key, fingerprint, httpContext.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, fingerprint, StringComparison.Ordinal))
            {
                throw IdempotencyKeyException.Reused(key);
            }

            if (!existing.IsCompleted)
            {
                throw IdempotencyKeyException.InProgress(key);
            }

            LogReplayed(_logger, key, customerId, existing.ResponseStatusCode!.Value);
            context.Result = new StoredResponseResult(existing.ResponseStatusCode.Value, existing.ResponseContentType ?? DefaultContentType, existing.ResponseBody ?? string.Empty);
            return;
        }

        await ExecuteAndStoreAsync(context, next, customerId, key);
    }

    /// <summary>
    /// Reserves the key, taking over a reservation that has no response and is older than the
    /// pending timeout (its request died before storing the result), provided it was for the same request.
    /// </summary>
    private async Task<IdempotencyKey?> ReserveAsync(int customerId, string key, string fingerprint, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existing = await _store.TryReserveAsync(customerId, key, fingerprint, now, cancellationToken);

        if (existing is { IsCompleted: false } stale
            && string.Equals(stale.RequestHash, fingerprint, StringComparison.Ordinal)
            && stale.CreatedAt <= now - _options.Value.PendingTimeout)
        {
            LogStaleReservation(_logger, key, customerId, stale.CreatedAt);
            await _store.ReleaseAsync(customerId, key, cancellationToken);
            existing = await _store.TryReserveAsync(customerId, key, fingerprint, now, cancellationToken);
        }

        return existing;
    }

    /// <summary>Runs the operation with the response buffered, stores a successful response under the key and releases the key otherwise.</summary>
    private async Task ExecuteAndStoreAsync(ResourceExecutingContext context, ResourceExecutionDelegate next, int customerId, string key)
    {
        var response = context.HttpContext.Response;
        var originalBody = response.Body;
        await using var buffer = new MemoryStream();
        response.Body = buffer;

        var stored = false;
        try
        {
            var executed = await next();
            var succeeded = executed.Exception is null || executed.ExceptionHandled;

            if (succeeded && response.StatusCode is >= 200 and < 300)
            {
                stored = await TryCompleteAsync(customerId, key, response.StatusCode, response.ContentType ?? DefaultContentType, Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
            }

            if (succeeded)
            {
                // Also starts the response (headers, including the correlation id, go out now).
                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody, context.HttpContext.RequestAborted);
            }

            // An unhandled exception is rethrown by MVC after this filter and answered by the exception handler.
        }
        finally
        {
            response.Body = originalBody;

            if (!stored)
            {
                await ReleaseQuietlyAsync(customerId, key);
            }
        }
    }

    /// <summary>Stores the response. The operation already happened, so a storage failure is logged and the response still goes out.</summary>
    private async Task<bool> TryCompleteAsync(int customerId, string key, int statusCode, string contentType, string body)
    {
        try
        {
            await _store.CompleteAsync(customerId, key, statusCode, contentType, body, CancellationToken.None);
            LogStored(_logger, key, customerId, statusCode);
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            LogStoreFailed(_logger, ex, key, customerId);
            return false;
        }
    }

    private async Task ReleaseQuietlyAsync(int customerId, string key)
    {
        try
        {
            await _store.ReleaseAsync(customerId, key, CancellationToken.None);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            LogReleaseFailed(_logger, ex, key, customerId);
        }
    }

    /// <summary>Hex SHA-256 of the method, the route and the raw body; the body stays readable for model binding.</summary>
    private static async Task<string> FingerprintAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        await using var body = new MemoryStream();
        await request.Body.CopyToAsync(body, cancellationToken);
        request.Body.Position = 0;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"{request.Method}\n{request.Path.Value?.ToUpperInvariant()}\n"));
        hash.AppendData(body.GetBuffer(), 0, (int)body.Length);

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    [LoggerMessage(EventId = 3100, Level = LogLevel.Information, Message = "Replayed the stored {StatusCode} response for idempotency key {IdempotencyKey} of customer {CustomerId}")]
    private static partial void LogReplayed(ILogger logger, string idempotencyKey, int customerId, int statusCode);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Debug, Message = "Stored the {StatusCode} response under idempotency key {IdempotencyKey} of customer {CustomerId}")]
    private static partial void LogStored(ILogger logger, string idempotencyKey, int customerId, int statusCode);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Warning, Message = "Taking over idempotency key {IdempotencyKey} of customer {CustomerId}: its reservation from {ReservedAt:O} never completed")]
    private static partial void LogStaleReservation(ILogger logger, string idempotencyKey, int customerId, DateTime reservedAt);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Error, Message = "The operation succeeded but its response could not be stored under idempotency key {IdempotencyKey} of customer {CustomerId}; the key was released")]
    private static partial void LogStoreFailed(ILogger logger, Exception exception, string idempotencyKey, int customerId);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Error, Message = "Idempotency key {IdempotencyKey} of customer {CustomerId} could not be released; it will be taken over after the pending timeout or purged by the cleanup")]
    private static partial void LogReleaseFailed(ILogger logger, Exception exception, string idempotencyKey, int customerId);
}
