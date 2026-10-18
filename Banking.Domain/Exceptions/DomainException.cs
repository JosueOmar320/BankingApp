namespace Banking.Domain.Exceptions;

/// <summary>
/// Raised when an operation violates a business rule (invalid amount, minimum deposit, etc.).
/// The API maps it to a <c>400 Bad Request</c> problem response whose text is chosen by
/// <see cref="Code"/>, one of <see cref="DomainErrorCodes"/>.
/// </summary>
public class DomainException : Exception, IHasErrorCode
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception with a stable code and the values its translated message needs.</summary>
    /// <param name="code">One of <see cref="DomainErrorCodes"/>.</param>
    /// <param name="message">English message for logs and as a fallback.</param>
    /// <param name="messageArguments">Values to format into the translated message.</param>
    public DomainException(string code, string message, params object?[] messageArguments)
        : base(message)
    {
        Code = code;
        MessageArguments = messageArguments;
    }

    /// <summary>Creates the exception with a stable code, an inner exception and the message values.</summary>
    protected DomainException(string code, string message, Exception innerException, IReadOnlyList<object?> messageArguments)
        : base(message, innerException)
    {
        Code = code;
        MessageArguments = messageArguments;
    }

    /// <inheritdoc />
    public string Code { get; } = DomainErrorCodes.BusinessRuleViolated;

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments { get; } = [];
}
