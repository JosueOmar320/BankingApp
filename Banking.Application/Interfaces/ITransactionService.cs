using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;

namespace Banking.Application.Interfaces;

/// <summary>Ledger use cases: deposits, withdrawals, interest and summaries.</summary>
public interface ITransactionService
{
    /// <summary>Deposits money into an account.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    /// <exception cref="DomainException">The amount violates a business rule.</exception>
    /// <exception cref="ConcurrencyConflictException">The account changed concurrently; retry.</exception>
    Task<TransactionResponseDto> DepositAsync(TransactionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Withdraws money from an account.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    /// <exception cref="DomainException">The amount violates a business rule.</exception>
    /// <exception cref="InsufficientFundsException">The amount exceeds the balance.</exception>
    /// <exception cref="ConcurrencyConflictException">The account changed concurrently; retry.</exception>
    Task<TransactionResponseDto> WithdrawAsync(TransactionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Credits the account's interest rate on its current balance.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    /// <exception cref="DomainException">The resulting interest is zero.</exception>
    /// <exception cref="ConcurrencyConflictException">The account changed concurrently; retry.</exception>
    Task<TransactionResponseDto> ApplyInterestAsync(ApplyInterestRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Returns the full ledger of an account with totals.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    Task<AccountTransactionSummaryDto> GetAccountTransactionSummaryAsync(string accountNumber, CancellationToken cancellationToken = default);
}
