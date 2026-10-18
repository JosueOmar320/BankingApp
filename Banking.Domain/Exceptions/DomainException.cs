namespace Banking.Domain.Exceptions;

/// <summary>
/// Raised when an operation violates a business rule (invalid amount, minimum deposit, etc.).
/// The API maps it to a <c>400 Bad Request</c> problem response.
/// </summary>
public class DomainException : Exception
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
}
