namespace Banking.Application.Dtos;

/// <summary>Totals of an account's whole ledger plus one page of its transactions.</summary>
/// <param name="AccountNumber">Account number.</param>
/// <param name="StartingBalance">Balance not explained by the ledger (zero unless the account predates the opening transaction).</param>
/// <param name="TotalDeposits">Sum of deposits over the whole ledger (including the opening deposit).</param>
/// <param name="TotalWithdrawals">Sum of withdrawals over the whole ledger.</param>
/// <param name="TotalInterest">Sum of interest credited over the whole ledger.</param>
/// <param name="TotalTransfersIn">Sum of transfers received from other accounts.</param>
/// <param name="TotalTransfersOut">Sum of transfers sent to other accounts.</param>
/// <param name="FinalBalance">Current balance.</param>
/// <param name="Transactions">The transactions on the requested page, oldest first.</param>
/// <param name="Page">Which page <paramref name="Transactions"/> is and how many there are.</param>
public sealed record AccountTransactionSummaryDto(
    string AccountNumber,
    decimal StartingBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    decimal TotalInterest,
    decimal TotalTransfersIn,
    decimal TotalTransfersOut,
    decimal FinalBalance,
    IReadOnlyList<TransactionSummaryDto> Transactions,
    PageInfoDto Page);
