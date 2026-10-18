namespace Banking.Domain.Exceptions;

/// <summary>Raised when a customer is registered with a document number that already exists.</summary>
public sealed class DuplicateDocumentNumberException : DomainException
{
    public DuplicateDocumentNumberException()
        : base(DomainErrorCodes.DuplicateDocumentNumber, "A customer with the same document number already exists.")
    {
    }

    public DuplicateDocumentNumberException(string message)
        : base(DomainErrorCodes.DuplicateDocumentNumber, message)
    {
    }

    public DuplicateDocumentNumberException(string message, Exception innerException)
        : base(DomainErrorCodes.DuplicateDocumentNumber, message, innerException, [])
    {
    }

    private DuplicateDocumentNumberException(string documentNumber, Exception innerException, IReadOnlyList<object?> messageArguments)
        : base(
            DomainErrorCodes.DuplicateDocumentNumber,
            $"A customer with document number '{documentNumber}' already exists.",
            innerException,
            messageArguments)
    {
        DocumentNumber = documentNumber;
    }

    /// <summary>The document number that already exists, when known.</summary>
    public string? DocumentNumber { get; }

    /// <summary>Creates the exception for a specific document number, so the translated message can mention it.</summary>
    public static DuplicateDocumentNumberException ForDocumentNumber(string documentNumber, Exception innerException)
        => new(documentNumber, innerException, [documentNumber]);
}
