namespace Banking.API.Localization;

/// <summary>Resource keys (in <c>ErrorMessages.resx</c>) of the problem response titles.</summary>
public static class ProblemTitles
{
    /// <summary>404: the customer or account does not exist.</summary>
    public const string NotFound = "Title.NotFound";

    /// <summary>409: another customer already has the document number.</summary>
    public const string DuplicateDocumentNumber = "Title.DuplicateDocumentNumber";

    /// <summary>409: the account changed concurrently; retry.</summary>
    public const string ConcurrencyConflict = "Title.ConcurrencyConflict";

    /// <summary>400: the withdrawal exceeds the balance.</summary>
    public const string InsufficientFunds = "Title.InsufficientFunds";

    /// <summary>400: any other business rule.</summary>
    public const string BusinessRuleViolated = "Title.BusinessRuleViolated";

    /// <summary>400: the payload failed validation.</summary>
    public const string ValidationFailed = "Title.ValidationFailed";

    /// <summary>499: the client went away.</summary>
    public const string RequestCancelled = "Title.RequestCancelled";

    /// <summary>500: unhandled exception.</summary>
    public const string UnexpectedError = "Title.UnexpectedError";
}
