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
    /// <summary>
    /// Service responsible for managing bank accounts, including creation, updates, and balance queries.
    /// </summary>
    public class BankAccountService : Interfaces.IBankAccountService
    {
        private readonly IBankAccountRepository _bankAccountRepository;

        /// <summary>
        /// Fixed interest rate applied to all bank accounts (10%).
        /// </summary>
        private const decimal InterestRate = 0.10m;

        /// <summary>
        /// Initializes a new instance of the <see cref="BankAccountService"/> class.
        /// </summary>
        /// <param name="bankAccountRepository">Repository for managing bank accounts.</param>
        public BankAccountService(IBankAccountRepository bankAccountRepository)
        {
            _bankAccountRepository = bankAccountRepository;
        }

        /// <summary>
        /// Creates a new bank account for a customer.
        /// </summary>
        /// <param name="bankAccountDto">DTO containing the bank account information.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The created bank account details including account number.</returns>
        public async Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default)
        {
            var account = new BankAccount
            {
                CustomerId = bankAccountDto.CustomerId,
                Balance = bankAccountDto.InitialDeposit,
                CreatedAt = DateTime.UtcNow,
                InterestRate = InterestRate,
                AccountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken)
            };

            var result = await _bankAccountRepository.AddBankAccountAsync(account, cancellationToken);

            return new BankAccountResponseDto{ AccountNumber = result.AccountNumber };
        }

        /// <summary>
        /// Updates an existing bank account.
        /// </summary>
        /// <param name="bankAccount">The bank account to update.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The updated bank account.</returns>
        public Task<BankAccount> UpdateBankAccountAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
            => _bankAccountRepository.UpdateBankAccountAsync(bankAccount, cancellationToken);

        /// <summary>
        /// Retrieves the balance for a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The balance of the account, or null if the account does not exist.</returns>
        public Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
            => _bankAccountRepository.GetBalanceByAccountNumberAsync(accountNumber, cancellationToken);

        /// <summary>
        /// Retrieves a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The bank account, or null if not found.</returns>
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
