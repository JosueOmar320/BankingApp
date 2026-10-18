namespace Banking.Infrastructure.Idempotency;

/// <summary>
/// One use of an <c>Idempotency-Key</c> by one customer. The row is inserted before the operation
/// runs (a reservation, so a concurrent duplicate cannot run too) and completed with the response
/// once the operation succeeded; a failed operation releases the row so the key can be retried.
/// </summary>
public sealed class IdempotencyKey
{
    /// <summary>Longest key accepted; longer headers are rejected before reaching the store.</summary>
    public const int MaxKeyLength = 128;

    /// <summary>Length of the hex SHA-256 fingerprint of the request.</summary>
    public const int RequestHashLength = 64;

    /// <summary>Longest content type stored with a response.</summary>
    public const int MaxContentTypeLength = 128;

    // EF Core materialisation.
    private IdempotencyKey()
    {
    }

    public IdempotencyKey(int customerId, string key, string requestHash, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        CustomerId = customerId;
        Key = key;
        RequestHash = requestHash;
        CreatedAt = createdAtUtc;
    }

    /// <summary>The authenticated customer who sent the key; keys of different customers never collide.</summary>
    public int CustomerId { get; private set; }

    /// <summary>The <c>Idempotency-Key</c> header value.</summary>
    public string Key { get; private set; } = null!;

    /// <summary>Hex SHA-256 of the method, route and body of the first request that used the key.</summary>
    public string RequestHash { get; private set; } = null!;

    /// <summary>Status code of the stored response; <see langword="null"/> while the first request is still running.</summary>
    public int? ResponseStatusCode { get; private set; }

    /// <summary>Content type of the stored response.</summary>
    public string? ResponseContentType { get; private set; }

    /// <summary>Body of the stored response, returned verbatim on a replay.</summary>
    public string? ResponseBody { get; private set; }

    /// <summary>UTC instant the key was first seen; rows older than the configured lifetime are purged.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Whether the first request finished and its response is available for replay.</summary>
    public bool IsCompleted => ResponseStatusCode.HasValue;

    /// <summary>Stores the response of the first request.</summary>
    public void Complete(int statusCode, string contentType, string body)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(body);

        ResponseStatusCode = statusCode;
        ResponseContentType = contentType;
        ResponseBody = body;
    }
}
