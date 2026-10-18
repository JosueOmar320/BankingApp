using Banking.API.Localization;
using Banking.Domain.Exceptions;
using Banking.Infrastructure.Idempotency;

namespace Banking.API.Idempotency;

/// <summary>
/// Raised by <see cref="IdempotencyFilter"/> when an <c>Idempotency-Key</c> cannot be honoured. It
/// carries the HTTP status and problem title the <c>GlobalExceptionHandler</c> uses, and a stable
/// code (see <see cref="ApiErrorCodes"/>) the handler translates into the response detail.
/// </summary>
public sealed class IdempotencyKeyException : Exception, IHasErrorCode
{
    /// <summary>Creates the exception with the default (key reused) mapping.</summary>
    public IdempotencyKeyException()
        : this(StatusCodes.Status422UnprocessableEntity, ProblemTitles.IdempotencyKeyReused, ApiErrorCodes.IdempotencyKeyReused, "The idempotency key was already used for a different request.", [])
    {
    }

    /// <summary>Creates the exception with a custom message and the default (key reused) mapping.</summary>
    public IdempotencyKeyException(string message)
        : this(StatusCodes.Status422UnprocessableEntity, ProblemTitles.IdempotencyKeyReused, ApiErrorCodes.IdempotencyKeyReused, message, [])
    {
    }

    /// <summary>Creates the exception with a custom message, an inner exception and the default (key reused) mapping.</summary>
    public IdempotencyKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity;
        TitleKey = ProblemTitles.IdempotencyKeyReused;
        Code = ApiErrorCodes.IdempotencyKeyReused;
        MessageArguments = [];
    }

    private IdempotencyKeyException(int statusCode, string titleKey, string code, string message, IReadOnlyList<object?> messageArguments)
        : base(message)
    {
        StatusCode = statusCode;
        TitleKey = titleKey;
        Code = code;
        MessageArguments = messageArguments;
    }

    /// <summary>HTTP status of the problem response.</summary>
    public int StatusCode { get; }

    /// <summary>Resource key of the problem title (see <see cref="ProblemTitles"/>).</summary>
    public string TitleKey { get; }

    /// <inheritdoc />
    public string Code { get; }

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments { get; }

    /// <summary>400: the header is empty, too long or contains characters other than visible ASCII.</summary>
    public static IdempotencyKeyException Invalid() => new(
        StatusCodes.Status400BadRequest,
        ProblemTitles.ValidationFailed,
        ApiErrorCodes.IdempotencyKeyInvalid,
        $"The {IdempotencyFilter.HeaderName} header must have between 1 and {IdempotencyKey.MaxKeyLength} visible ASCII characters.",
        [IdempotencyKey.MaxKeyLength]);

    /// <summary>422: the key was already used by this customer for a request with another body or route.</summary>
    public static IdempotencyKeyException Reused(string key) => new(
        StatusCodes.Status422UnprocessableEntity,
        ProblemTitles.IdempotencyKeyReused,
        ApiErrorCodes.IdempotencyKeyReused,
        $"Idempotency key '{key}' was already used for a different request.",
        [key]);

    /// <summary>409: the first request with the key has not finished yet.</summary>
    public static IdempotencyKeyException InProgress(string key) => new(
        StatusCodes.Status409Conflict,
        ProblemTitles.IdempotencyKeyInProgress,
        ApiErrorCodes.IdempotencyKeyInProgress,
        $"A request with idempotency key '{key}' is still being processed.",
        [key]);
}
