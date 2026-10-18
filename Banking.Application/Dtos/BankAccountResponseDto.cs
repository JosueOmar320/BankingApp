namespace Banking.Application.Dtos;

/// <summary>A bank account.</summary>
/// <param name="AccountNumber">Ten-digit account number.</param>
/// <param name="CustomerId">Owner of the account.</param>
/// <param name="Balance">Current balance.</param>
/// <param name="InterestRate">Interest rate applied by the apply-interest operation (0.10 = 10%).</param>
/// <param name="CreatedAt">UTC opening timestamp.</param>
public sealed record BankAccountResponseDto(
    string AccountNumber,
    int CustomerId,
    decimal Balance,
    decimal InterestRate,
    DateTime CreatedAt);
