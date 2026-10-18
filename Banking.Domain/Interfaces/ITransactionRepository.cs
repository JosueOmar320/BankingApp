using Banking.Domain.Entities;

namespace Banking.Domain.Interfaces;

/// <summary>Read-side access to the ledger. Writes always go through <see cref="BankAccount"/>.</summary>
public interface ITransactionRepository
{
    /// <summary>All transactions of an account, oldest first.</summary>
    Task<IReadOnlyList<Transaction>> GetAllByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default);
}
