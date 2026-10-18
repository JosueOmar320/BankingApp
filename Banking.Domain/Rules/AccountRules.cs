namespace Banking.Domain.Rules;

/// <summary>
/// Business constants that apply to every bank account. They live in the domain so that
/// every entry point (API, seeders, tests) enforces exactly the same rules.
/// </summary>
public static class AccountRules
{
    /// <summary>Minimum amount required to open an account.</summary>
    public const decimal MinimumInitialDeposit = 10m;

    /// <summary>Fixed annual interest rate applied to every account (10%).</summary>
    public const decimal InterestRate = 0.10m;

    /// <summary>Number of decimal places money amounts are stored with.</summary>
    public const int MoneyScale = 2;

    /// <summary>Length of generated account numbers.</summary>
    public const int AccountNumberLength = 10;
}
