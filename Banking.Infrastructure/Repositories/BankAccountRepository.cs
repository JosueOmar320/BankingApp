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
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly BankingDbContext _context;
        public BankAccountRepository(BankingDbContext context)
        {
            _context = context;
        }

        public async Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            await _context.BankAccounts.AddAsync(account, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return account;
        }

        public Task<BankAccount?> GetBankAccountByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
            => _context.BankAccounts.FirstOrDefaultAsync(b => b.AccountNumber.Equals(accountNumber), cancellationToken);
    }
}
