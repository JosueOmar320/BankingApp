using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

/// <summary>One ledger entry inside an account summary.</summary>
/// <param name="TransactionId">Identifier of the ledger entry.</param>
/// <param name="TransactionType">Kind of movement.</param>
/// <param name="Amount">Amount moved (always positive).</param>
/// <param name="BalanceAfter">Balance right after the movement.</param>
/// <param name="TransactionDate">UTC timestamp of the movement.</param>
public sealed record TransactionSummaryDto(
    int TransactionId,
    TransactionType TransactionType,
    decimal Amount,
    decimal BalanceAfter,
    DateTime TransactionDate);
