namespace Banking.Application.Exceptions;

/// <summary>
/// Stable identifiers for the errors raised by the application layer, complementing
/// <see cref="Banking.Domain.Exceptions.DomainErrorCodes"/>. The API translates them into messages.
/// </summary>
public static class ApplicationErrorCodes
{
    /// <summary>No customer has the given identifier. Arguments: customer id.</summary>
    public const string CustomerNotFound = nameof(CustomerNotFound);

    /// <summary>No account has the given number. Arguments: account number.</summary>
    public const string AccountNotFound = nameof(AccountNotFound);

    /// <summary>The account belongs to another customer. Arguments: account number.</summary>
    public const string AccountAccessDenied = nameof(AccountAccessDenied);

    /// <summary>The customer record belongs to another customer. Arguments: customer id.</summary>
    public const string CustomerAccessDenied = nameof(CustomerAccessDenied);

    /// <summary>The document number or the password of a login attempt is wrong.</summary>
    public const string InvalidCredentials = nameof(InvalidCredentials);
}
