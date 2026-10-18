using Banking.Domain.Entities;

namespace Banking.Domain.ValueObjects;

/// <summary>One page of an account's ledger, oldest transaction first.</summary>
/// <param name="Transactions">The transactions on this page.</param>
/// <param name="Page">1-based page number that was requested.</param>
/// <param name="PageSize">Maximum number of transactions per page.</param>
/// <param name="TotalCount">Number of transactions in the whole ledger.</param>
public sealed record LedgerPage(IReadOnlyList<Transaction> Transactions, int Page, int PageSize, int TotalCount)
{
    /// <summary>Number of pages the ledger spans at <see cref="PageSize"/>; zero for an empty ledger.</summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
