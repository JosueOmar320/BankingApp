namespace Banking.Application.Dtos;

/// <summary>Balance of a bank account.</summary>
/// <param name="AccountNumber">Ten-digit account number.</param>
/// <param name="Balance">Current balance.</param>
public sealed record AccountBalanceDto(string AccountNumber, decimal Balance);
