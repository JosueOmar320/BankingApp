namespace Banking.Domain.Exceptions;

/// <summary>
/// Stable, language-neutral identifiers for every business rule the domain can reject.
/// The domain never produces user-facing text: it raises an exception carrying one of these
/// codes plus the values involved, and the outer layers translate the code into a message in the
/// caller's language. Clients can rely on the codes; the wording may change.
/// </summary>
public static class DomainErrorCodes
{
    /// <summary>Fallback for a <see cref="DomainException"/> created without a specific code.</summary>
    public const string BusinessRuleViolated = nameof(BusinessRuleViolated);

    /// <summary>A withdrawal exceeds the available balance. Arguments: balance, requested amount.</summary>
    public const string InsufficientFunds = nameof(InsufficientFunds);

    /// <summary>The opening deposit is below the minimum. Arguments: minimum deposit.</summary>
    public const string DepositBelowMinimum = nameof(DepositBelowMinimum);

    /// <summary>A money amount is zero or negative.</summary>
    public const string AmountNotPositive = nameof(AmountNotPositive);

    /// <summary>A money amount has more decimals than allowed. Arguments: allowed decimals.</summary>
    public const string AmountTooManyDecimals = nameof(AmountTooManyDecimals);

    /// <summary>The interest to credit rounds to zero.</summary>
    public const string InterestRoundsToZero = nameof(InterestRoundsToZero);

    /// <summary>An account was opened without an account number.</summary>
    public const string AccountNumberRequired = nameof(AccountNumberRequired);

    /// <summary>An account was opened without a valid customer.</summary>
    public const string CustomerRequired = nameof(CustomerRequired);

    /// <summary>The customer's first name is missing.</summary>
    public const string FirstNameRequired = nameof(FirstNameRequired);

    /// <summary>The customer's first name is too long. Arguments: maximum length.</summary>
    public const string FirstNameTooLong = nameof(FirstNameTooLong);

    /// <summary>The customer's last name is too long. Arguments: maximum length.</summary>
    public const string LastNameTooLong = nameof(LastNameTooLong);

    /// <summary>The customer's document number is missing.</summary>
    public const string DocumentNumberRequired = nameof(DocumentNumberRequired);

    /// <summary>The customer's document number is too long. Arguments: maximum length.</summary>
    public const string DocumentNumberTooLong = nameof(DocumentNumberTooLong);

    /// <summary>The gender is not one of the supported values.</summary>
    public const string InvalidGender = nameof(InvalidGender);

    /// <summary>The birth date is later than today.</summary>
    public const string BirthDateInFuture = nameof(BirthDateInFuture);

    /// <summary>The monthly income is negative.</summary>
    public const string NegativeMonthlyIncome = nameof(NegativeMonthlyIncome);

    /// <summary>The login password is missing.</summary>
    public const string PasswordRequired = nameof(PasswordRequired);

    /// <summary>The login password is too short. Arguments: minimum length.</summary>
    public const string PasswordTooShort = nameof(PasswordTooShort);

    /// <summary>The login password is too long. Arguments: maximum length.</summary>
    public const string PasswordTooLong = nameof(PasswordTooLong);

    /// <summary>Another customer already has the document number. Arguments: document number.</summary>
    public const string DuplicateDocumentNumber = nameof(DuplicateDocumentNumber);

    /// <summary>The account changed between read and save. Arguments: account number.</summary>
    public const string ConcurrencyConflict = nameof(ConcurrencyConflict);

    /// <summary>A generated account number was taken by a concurrent request. Arguments: account number.</summary>
    public const string AccountNumberTaken = nameof(AccountNumberTaken);
}
