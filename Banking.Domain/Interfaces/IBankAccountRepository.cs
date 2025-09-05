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
        Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
        Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
