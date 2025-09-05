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
    public class TransactionRepository : ITransactionRepository
    {
        private readonly BankingDbContext _context;
        public TransactionRepository(BankingDbContext context)
        {
            _context = context;
        }
        public async Task<Transaction> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default)
        {
            return await _context.Transactions
                .Where(t => t.BankAccountId.Equals(bankAccountId))
                .OrderBy(t => t.TransactionDate)
                .ToListAsync(cancellationToken);
        }
    }
}
