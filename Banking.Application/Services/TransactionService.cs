using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <inheritdoc cref="ITransactionService"/>
internal sealed class TransactionService : ITransactionService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IBankAccountRepository bankAccountRepository,
        ITransactionRepository transactionRepository,
        TimeProvider timeProvider,
        ILogger<TransactionService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _transactionRepository = transactionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task<TransactionResponseDto> DepositAsync(TransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApplyAsync(request.AccountNumber, account => account.Deposit(request.Amount, Now()), cancellationToken);
    }

    public Task<TransactionResponseDto> WithdrawAsync(TransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApplyAsync(request.AccountNumber, account => account.Withdraw(request.Amount, Now()), cancellationToken);
    }

    public Task<TransactionResponseDto> ApplyInterestAsync(ApplyInterestRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApplyAsync(request.AccountNumber, account => account.ApplyInterest(Now()), cancellationToken);
    }

    public async Task<AccountTransactionSummaryDto> GetAccountTransactionSummaryAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await LoadAccountAsync(accountNumber, cancellationToken);
        var transactions = await _transactionRepository.GetAllByAccountIdAsync(account.BankAccountId, cancellationToken);

        decimal deposits = 0, withdrawals = 0, interest = 0;
        foreach (var transaction in transactions)
        {
            switch (transaction.TransactionType)
            {
                case TransactionType.Deposit:
                    deposits += transaction.Amount;
                    break;
                case TransactionType.Withdrawal:
                    withdrawals += transaction.Amount;
                    break;
                case TransactionType.Interest:
                    interest += transaction.Amount;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown transaction type '{transaction.TransactionType}'.");
            }
        }

        return new AccountTransactionSummaryDto(
            account.AccountNumber,
            StartingBalance: account.Balance + withdrawals - deposits - interest,
            TotalDeposits: deposits,
            TotalWithdrawals: withdrawals,
            TotalInterest: interest,
            FinalBalance: account.Balance,
            Transactions: transactions.Select(t => t.ToSummaryDto()).ToList());
    }

    /// <summary>
    /// Loads the account, applies one ledger operation and saves balance + transaction in a single unit of work.
    /// </summary>
    private async Task<TransactionResponseDto> ApplyAsync(
        string accountNumber,
        Func<BankAccount, Transaction> operation,
        CancellationToken cancellationToken)
    {
        var account = await LoadAccountAsync(accountNumber, cancellationToken);
        var transaction = operation(account);

        await _bankAccountRepository.UpdateBankAccountAsync(account, cancellationToken);

        Log.TransactionApplied(_logger, transaction.TransactionType, transaction.Amount, account.BankAccountId, account.Balance);

        return transaction.ToDto(account.AccountNumber);
    }

    private async Task<BankAccount> LoadAccountAsync(string accountNumber, CancellationToken cancellationToken)
        => await _bankAccountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken)
            ?? throw new NotFoundException("Account", accountNumber);

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}
