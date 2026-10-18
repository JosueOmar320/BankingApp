namespace Banking.Application.Dtos;

/// <summary>Ledger of an account with totals.</summary>
/// <param name="AccountNumber">Account number.</param>
/// <param name="StartingBalance">Balance before the first listed transaction.</param>
/// <param name="TotalDeposits">Sum of deposits (including the opening deposit).</param>
/// <param name="TotalWithdrawals">Sum of withdrawals.</param>
/// <param name="TotalInterest">Sum of interest credited.</param>
/// <param name="FinalBalance">Current balance.</param>
/// <param name="Transactions">Transactions, oldest first.</param>
public sealed record AccountTransactionSummaryDto(
    string AccountNumber,
    decimal StartingBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalInterest,
    decimal FinalBalance,
    IReadOnlyList<TransactionSummaryDto> Transactions);
