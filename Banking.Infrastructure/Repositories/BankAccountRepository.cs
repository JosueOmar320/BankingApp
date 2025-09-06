using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for handling bank account operations in the database.
    /// </summary>
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly BankingDbContext _context;

        /// <summary>
        /// Constructor that receives the database context.
        /// </summary>
        /// <param name="context">Instance of <see cref="BankingDbContext"/> for database access.</param>
        public BankAccountRepository(BankingDbContext context)
        {
            _context = context;
        }

        // <summary>
        /// Adds a new bank account to the database.
        /// </summary>
        /// <param name="account">The <see cref="BankAccount"/> entity to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The added bank account.</returns>
        public async Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            await _context.BankAccounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return account;
        }

        /// <summary>
        /// Gets the balance of a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number to query.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The account balance or null if the account does not exist.</returns>                                           
        public async Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = await _context.BankAccounts
                            .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber, cancellationToken);

            return account?.Balance;
        }

        /// <summary>
        /// Gets the full information of a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number to query.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The <see cref="BankAccount"/> entity or null if it does not exist.</returns>
        public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber, cancellationToken);
        }

        /// <summary>
        /// Updates a bank account in the database.
        /// </summary>
        /// <param name="account">The <see cref="BankAccount"/> entity with changes to update.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account.</returns>
        public async Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            _context.BankAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
            return account;
        }

        /// <summary>
        /// Checks if a bank account exists with a specific account number.
        /// </summary>
        /// <param name="accountNumber">The account number to check.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the account exists; false otherwise.</returns>
        public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken)
            => _context.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }
}
