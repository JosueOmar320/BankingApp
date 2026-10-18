namespace Banking.Domain.Exceptions;

/// <summary>
/// Raised when an account was modified by another request between the time it was read
/// and the time the change was saved. The caller should reload the account and retry.
/// </summary>
public sealed class ConcurrencyConflictException : Exception, IHasErrorCode
{
    public ConcurrencyConflictException()
        : base("The account was modified by another operation. Please retry.")
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private ConcurrencyConflictException(string code, string message, Exception innerException, IReadOnlyList<object?> messageArguments)
        : base(message, innerException)
    {
        Code = code;
        MessageArguments = messageArguments;
    }

    /// <inheritdoc />
    public string Code { get; } = DomainErrorCodes.ConcurrencyConflict;

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments { get; } = [];

    /// <summary>The account was changed by another operation while this one was in flight.</summary>
    public static ConcurrencyConflictException ForAccount(string accountNumber, Exception innerException)
        => new(
            DomainErrorCodes.ConcurrencyConflict,
            $"Account '{accountNumber}' was modified by another operation. Please retry.",
            innerException,
            [accountNumber]);

    /// <summary>A freshly generated account number was taken by a concurrent request.</summary>
    public static ConcurrencyConflictException AccountNumberTaken(string accountNumber, Exception innerException)
        => new(
            DomainErrorCodes.AccountNumberTaken,
            $"Account number '{accountNumber}' was taken by a concurrent request. Please retry.",
            innerException,
            [accountNumber]);
}
