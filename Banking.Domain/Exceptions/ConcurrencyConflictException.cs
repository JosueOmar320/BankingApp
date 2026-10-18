namespace Banking.Domain.Exceptions;

/// <summary>
/// Raised when an account was modified by another request between the time it was read
/// and the time the change was saved. The caller should reload the account and retry.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
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
}
