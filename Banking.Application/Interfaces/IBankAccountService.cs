using Banking.Application.Dtos;
using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing bank account operations.
    /// </summary>
    public interface IBankAccountService
    {
        /// <summary>
        /// Creates a new bank account for a customer.
        /// </summary>
        /// <param name="bankAccountDto">DTO containing the bank account information.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The created bank account details including account number.</returns>
        Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing bank account.
        /// </summary>
        /// <param name="bankAccount">The bank account to update.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The updated bank account.</returns>
        Task<BankAccount> UpdateBankAccountAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The bank account, or null if not found.</returns>
        Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the balance for a bank account by its account number.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The balance of the account, or null if the account does not exist.</returns>
        Task<decimal?> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
