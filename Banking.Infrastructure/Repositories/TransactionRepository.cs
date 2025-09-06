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
    /// Repository for handling transactions in the database.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BankingDbContext _context;

        /// <summary>
        /// Constructor that receives the database context.
        /// </summary>
        /// <param name="context">Instance of <see cref="BankingDbContext"/> for database access.</param>
        public TransactionRepository(BankingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a new transaction to the database.
        /// </summary>
        /// <param name="transaction">The <see cref="Transaction"/> entity to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The added transaction.</returns>
        public async Task<Transaction> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return transaction;
        }

        /// <summary>
        /// Gets all transactions for a given bank account, ordered by date.
        /// </summary>
        /// <param name="bankAccountId">The ID of the bank account.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of <see cref="Transaction"/> entities for the account.</returns>
        public async Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .Where(t => t.BankAccountId == bankAccountId)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync(cancellationToken);
        }
    }
}
