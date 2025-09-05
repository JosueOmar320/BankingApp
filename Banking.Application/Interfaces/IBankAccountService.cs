using Banking.Application.Dtos;
using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Interfaces
{
    public interface IBankAccountService
    {
        Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default);
        Task<BankAccount> UpdateBankAccountAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
        Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
        Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
