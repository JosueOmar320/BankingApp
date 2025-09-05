using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Infrastructure.Repositories
{
    public class BankAccountRepository : IBankAccountRepository
    {
        public Task AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BankAccount> GetBankAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
