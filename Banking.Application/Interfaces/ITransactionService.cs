using Banking.Application.Dtos;
using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponseDto?> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);
        Task<TransactionResponseDto?> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default);
        Task<TransactionResponseDto?> ApplyInterestAsync(string accountNumber, CancellationToken cancellationToken = default);
        Task<AccountTransactionSummaryDto?> GetAccountTransactionSummaryAsync(string accountNumber, CancellationToken cancellationToken = default);
    }
}
