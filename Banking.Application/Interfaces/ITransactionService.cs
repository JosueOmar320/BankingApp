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
    /// Service interface for managing transactions such as deposits, withdrawals, and interest application.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Deposits a specified amount into a bank account.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="amount">The amount to deposit. Must be greater than zero.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The transaction details including updated balance, or null if the account does not exist.</returns>
        /// <exception cref="ArgumentException">Thrown if amount is less than or equal to zero.</exception>
        Task<TransactionResponseDto?> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Withdraws a specified amount from a bank account.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="amount">The amount to withdraw. Must be greater than zero.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The transaction details including updated balance, or null if the account does not exist.</returns>
        /// <exception cref="ArgumentException">Thrown if amount is less than or equal to zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown if account balance is insufficient.</exception>
        Task<TransactionResponseDto?> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies interest to a bank account based on its current balance and fixed interest rate.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The transaction details including updated balance, or null if the account does not exist.</returns>
        Task<TransactionResponseDto?> ApplyInterestAsync(string accountNumber, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieves a summary of all transactions for a given account, including deposits, withdrawals, interests, starting and final balances.
        /// </summary>
        /// <param name="accountNumber">The account number of the bank account.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>An account transaction summary, or null if the account does not exist.</returns>
        Task<AccountTransactionSummaryDto?> GetAccountTransactionSummaryAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
