using Banking.Domain.ValueObjects;

namespace Banking.Domain.Interfaces;

/// <summary>Read-side access to the ledger. Writes always go through <see cref="Entities.BankAccount"/>.</summary>
public interface ITransactionRepository
{
    /// <summary>One page of an account's transactions, oldest first.</summary>
    /// <param name="bankAccountId">Owning account.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Transactions per page, at least 1.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<LedgerPage> GetPageByAccountIdAsync(int bankAccountId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Amount moved by each transaction type over the account's whole ledger.</summary>
    Task<LedgerTotals> GetTotalsByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default);
}
