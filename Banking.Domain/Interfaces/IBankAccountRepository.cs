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
        Task<List<BankAccount>> GetAllBankAccountsAsync();
        Task<BankAccount> GetBankAccountByIdAsync(Guid accountId);
        Task AddBankAccountAsync(BankAccount account);
        Task UpdateBankAccountAsync(BankAccount account);
    }
}
