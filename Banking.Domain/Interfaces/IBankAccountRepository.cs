using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing bank accounts in the persistence layer.
    /// </summary>
    public interface IBankAccountRepository
    {
        /// <summary>
        /// Gets the balance of a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number to query.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The account balance or null if the account does not exist.</returns>   
        Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

        // <summary>
        /// Adds a new bank account to the database.
        /// </summary>
        /// <param name="account">The <see cref="BankAccount"/> entity to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The added bank account.</returns>
        Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the full information of a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number to query.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The <see cref="BankAccount"/> entity or null if it does not exist.</returns>
        Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a bank account in the database.
        /// </summary>
        /// <param name="account">The <see cref="BankAccount"/> entity with changes to update.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account.</returns>
        Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a bank account exists with a specific account number.
        /// </summary>
        /// <param name="accountNumber">The account number to check.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the account exists; false otherwise.</returns>
        Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
