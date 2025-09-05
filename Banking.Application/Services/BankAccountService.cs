using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly decimal _interestRate;

        public BankAccountService(IBankAccountRepository bankAccountRepository)
        {
            _bankAccountRepository = bankAccountRepository;
            _interestRate = 0.10m; // Example interest rate
        }

        public async Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default)
        {
            var account = new BankAccount
            {
                CustomerId = bankAccountDto.CustomerId,
                Balance = bankAccountDto.InitialDeposit,
                CreatedAt = DateTime.UtcNow,
                InterestRate = _interestRate,
                AccountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken)
            };

            var result = await _bankAccountRepository.AddBankAccountAsync(account, cancellationToken);

            return new BankAccountResponseDto{ AccountNumber = result.AccountNumber };
        }

        public Task<BankAccount> UpdateBankAccountAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
            => _bankAccountRepository.UpdateBankAccountAsync(bankAccount, cancellationToken);

        public Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
            => _bankAccountRepository.GetBalanceByAccountNumberAsync(accountNumber, cancellationToken);

        public Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
            => _bankAccountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);

        private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken = default)
        {
            string accountNumber;
            do
            {
                accountNumber = new Random().Next(1000000000, int.MaxValue).ToString(); 
            }
            while (await _bankAccountRepository.ExistsByNumberAsync(accountNumber, cancellationToken));

            return accountNumber;
        }
    }
}
