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
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IBankAccountRepository bankAccountRepository,
        ITransactionRepository transactionRepository,
        ICurrentCustomer currentCustomer,
        TimeProvider timeProvider,
        ILogger<TransactionService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _transactionRepository = transactionRepository;
        _currentCustomer = currentCustomer;
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

    public async Task<TransferResponseDto> TransferAsync(TransferRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The source must be the caller's (404/403); the destination only has to exist (404).
        var source = await LoadAccountAsync(request.SourceAccountNumber, cancellationToken);
        var destination = await _bankAccountRepository.GetByAccountNumberAsync(request.DestinationAccountNumber, cancellationToken)
            ?? throw new NotFoundException("Account", request.DestinationAccountNumber);

        var transfer = source.TransferTo(destination, request.Amount, Now());

        await _bankAccountRepository.SaveTransferAsync(source, destination, cancellationToken);

        Log.TransferApplied(_logger, request.Amount, source.BankAccountId, destination.BankAccountId, source.Balance);

        return new TransferResponseDto(
            transfer.Debit.TransactionId,
            source.AccountNumber,
            destination.AccountNumber,
            request.Amount,
            SourceBalanceAfter: source.Balance,
            transfer.Debit.TransactionDate);
    }

    public async Task<AccountTransactionSummaryDto> GetAccountTransactionSummaryAsync(string accountNumber, LedgerPageRequestDto page, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        var account = await LoadAccountAsync(accountNumber, cancellationToken);
        var totals = await _transactionRepository.GetTotalsByAccountIdAsync(account.BankAccountId, cancellationToken);
        var ledgerPage = await _transactionRepository.GetPageByAccountIdAsync(account.BankAccountId, page.Page, page.PageSize, cancellationToken);

        var deposits = totals.Of(TransactionType.Deposit);
        var withdrawals = totals.Of(TransactionType.Withdrawal);
        var interest = totals.Of(TransactionType.Interest);
        var transfersIn = totals.Of(TransactionType.TransferIn);
        var transfersOut = totals.Of(TransactionType.TransferOut);

        return new AccountTransactionSummaryDto(
            account.AccountNumber,
            StartingBalance: account.Balance + withdrawals + transfersOut - deposits - interest - transfersIn,
            TotalDeposits: deposits,
            TotalWithdrawals: withdrawals,
            TotalInterest: interest,
            TotalTransfersIn: transfersIn,
            TotalTransfersOut: transfersOut,
            FinalBalance: account.Balance,
            Transactions: ledgerPage.Transactions.Select(t => t.ToSummaryDto()).ToList(),
            Page: new PageInfoDto(ledgerPage.Page, ledgerPage.PageSize, ledgerPage.TotalCount, ledgerPage.TotalPages));
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

    /// <summary>Loads an account that must exist (404) and belong to the caller (403).</summary>
    private async Task<BankAccount> LoadAccountAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken)
            ?? throw new NotFoundException("Account", accountNumber);
        account.EnsureOwnedBy(_currentCustomer);

        return account;
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;
}
