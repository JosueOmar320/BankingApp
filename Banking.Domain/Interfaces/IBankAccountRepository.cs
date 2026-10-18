using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Interfaces;

/// <summary>Persistence operations for the <see cref="BankAccount"/> aggregate.</summary>
public interface IBankAccountRepository
{
    /// <summary>Gets an account (tracked, ready to be modified) or <see langword="null"/>.</summary>
    Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Reads only the balance of an account, or <see langword="null"/> when it does not exist.</summary>
    Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Checks whether an account number is already taken.</summary>
    Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Persists a new account together with its opening transaction.</summary>
    /// <exception cref="ConcurrencyConflictException">The account number was taken concurrently.</exception>
    Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

    /// <summary>Saves the account's new balance and any transactions it recorded, atomically.</summary>
    /// <exception cref="ConcurrencyConflictException">The account changed since it was read.</exception>
    Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);
}
