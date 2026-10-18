namespace Banking.Domain.Exceptions;

/// <summary>Raised when a customer is registered with a document number that already exists.</summary>
public sealed class DuplicateDocumentNumberException : DomainException
{
    public DuplicateDocumentNumberException()
        : base("A customer with the same document number already exists.")
    {
    }

    public DuplicateDocumentNumberException(string message)
        : base(message)
    {
    }

    public DuplicateDocumentNumberException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
