using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
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
        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.Transactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
