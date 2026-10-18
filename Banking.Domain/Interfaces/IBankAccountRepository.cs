using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Interfaces;

/// <summary>Persistence operations for the <see cref="BankAccount"/> aggregate.</summary>
public interface IBankAccountRepository
{
    /// <summary>Gets an account (tracked, ready to be modified) or <see langword="null"/>.</summary>
    Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Gets every account of a customer, oldest first (read-only, not tracked). Empty when the customer has none.</summary>
    Task<IReadOnlyList<BankAccount>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an account number is already taken.</summary>
    Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Persists a new account together with its opening transaction.</summary>
    /// <exception cref="ConcurrencyConflictException">The account number was taken concurrently.</exception>
    Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>Saves the account's new balance and any transactions it recorded, atomically.</summary>
    /// <exception cref="ConcurrencyConflictException">The account changed since it was read.</exception>
    Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>Saves both sides of a transfer (balances and ledger entries) in one unit of work.</summary>
    /// <exception cref="ConcurrencyConflictException">Either account changed since it was read; nothing is saved.</exception>
    Task SaveTransferAsync(BankAccount source, BankAccount destination, CancellationToken cancellationToken = default);
}
