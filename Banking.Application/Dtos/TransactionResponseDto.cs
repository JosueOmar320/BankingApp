using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

/// <summary>Result of a deposit, withdrawal or interest operation.</summary>
/// <param name="TransactionId">Identifier of the ledger entry.</param>
/// <param name="AccountNumber">Account the movement was applied to.</param>
/// <param name="TransactionType">Kind of movement.</param>
/// <param name="Amount">Amount moved (always positive).</param>
/// <param name="BalanceAfter">Balance right after the movement.</param>
/// <param name="TransactionDate">UTC timestamp of the movement.</param>
public sealed record TransactionResponseDto(
    int TransactionId,
    string AccountNumber,
    TransactionType TransactionType,
    decimal Amount,
    decimal BalanceAfter,
    DateTime TransactionDate);
