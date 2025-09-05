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

        public async Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = await _context.BankAccounts
                            .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber, cancellationToken);

            return account?.Balance;
        }

        public async Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            return await _context.BankAccounts
                .FirstOrDefaultAsync(b => b.AccountNumber == accountNumber, cancellationToken);
        }

        public async Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            _context.BankAccounts.Update(account);
            await _context.SaveChangesAsync(cancellationToken);
            return account;
        }

        public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken)
            => _context.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }
}
