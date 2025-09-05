using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<BankAccount> GetBankAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
        Task AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);
        Task UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);
    }
}
