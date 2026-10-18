using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banking.Tests.Application.Services;

public class TransactionServiceTests
{
    private Mock<IBankAccountRepository> _accounts = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private FakeCurrentCustomer _caller = null!;
    private TransactionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IBankAccountRepository>(MockBehavior.Strict);
        _transactions = new Mock<ITransactionRepository>(MockBehavior.Strict);
        _caller = new FakeCurrentCustomer(1);
        _service = new TransactionService(_accounts.Object, _transactions.Object, _caller, new FixedTimeProvider(), NullLogger<TransactionService>.Instance);
    }

    [Test]
    public void EveryOperation_ThrowsForbiddenForAnotherCustomersAccount_WithoutSaving()
    {
        var account = ArrangeAccount(100m, customerId: 2);
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.ThrowsAsync<ForbiddenException>(() => _service.DepositAsync(request))!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(Assert.ThrowsAsync<ForbiddenException>(() => _service.WithdrawAsync(request))!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(Assert.ThrowsAsync<ForbiddenException>(() => _service.ApplyInterestAsync(new ApplyInterestRequestDto { AccountNumber = account.AccountNumber }))!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(Assert.ThrowsAsync<ForbiddenException>(() => _service.GetAccountTransactionSummaryAsync(account.AccountNumber, new LedgerPageRequestDto()))!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(account.Balance, Is.EqualTo(100m));
        });
        _accounts.Verify(a => a.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private BankAccount ArrangeAccount(decimal initialDeposit = 100m, int customerId = 1, string accountNumber = "1234567890")
    {
        var account = TestData.Account(customerId, initialDeposit, accountNumber);
        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accounts.Setup(a => a.UpdateBankAccountAsync(account, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        return account;
    }

    [Test]
    public async Task DepositAsync_AppliesToAccountAndSavesOnce()
    {
        var account = ArrangeAccount(100m);

        var result = await _service.DepositAsync(new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 50m });

        Assert.Multiple(() =>
        {
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(result.Amount, Is.EqualTo(50m));
            Assert.That(result.BalanceAfter, Is.EqualTo(150m));
            Assert.That(result.AccountNumber, Is.EqualTo(account.AccountNumber));
            Assert.That(result.TransactionDate, Is.EqualTo(TestData.Now));
            Assert.That(account.Balance, Is.EqualTo(150m));
        });
        _accounts.Verify(a => a.UpdateBankAccountAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WithdrawAsync_AppliesToAccountAndSavesOnce()
    {
        var account = ArrangeAccount(200m);

        var result = await _service.WithdrawAsync(new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 50m });

        Assert.Multiple(() =>
        {
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Withdrawal));
            Assert.That(result.BalanceAfter, Is.EqualTo(150m));
        });
        _accounts.Verify(a => a.UpdateBankAccountAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void WithdrawAsync_InsufficientFunds_DoesNotSave()
    {
        var account = ArrangeAccount(20m);

        Assert.ThrowsAsync<InsufficientFundsException>(() => _service.WithdrawAsync(new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 50m }));
        _accounts.Verify(a => a.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void DepositAsync_InvalidAmount_DoesNotSave(decimal amount)
    {
        var account = ArrangeAccount();

        Assert.ThrowsAsync<DomainException>(() => _service.DepositAsync(new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = amount }));
        _accounts.Verify(a => a.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ApplyInterestAsync_CreditsRate()
    {
        var account = ArrangeAccount(100m);

        var result = await _service.ApplyInterestAsync(new ApplyInterestRequestDto { AccountNumber = account.AccountNumber });

        Assert.Multiple(() =>
        {
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Interest));
            Assert.That(result.Amount, Is.EqualTo(10m));
            Assert.That(result.BalanceAfter, Is.EqualTo(110m));
        });
    }

    [Test]
    public void AnyOperation_ThrowsNotFoundForUnknownAccount()
    {
        _accounts.Setup(a => a.GetByAccountNumberAsync("999", It.IsAny<CancellationToken>())).ReturnsAsync((BankAccount?)null);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<NotFoundException>(() => _service.DepositAsync(new TransactionRequestDto { AccountNumber = "999", Amount = 1m }));
            Assert.ThrowsAsync<NotFoundException>(() => _service.WithdrawAsync(new TransactionRequestDto { AccountNumber = "999", Amount = 1m }));
            Assert.ThrowsAsync<NotFoundException>(() => _service.ApplyInterestAsync(new ApplyInterestRequestDto { AccountNumber = "999" }));
            Assert.ThrowsAsync<NotFoundException>(() => _service.GetAccountTransactionSummaryAsync("999", new LedgerPageRequestDto()));
        });
    }

    [Test]
    public void DepositAsync_PropagatesConcurrencyConflict()
    {
        var account = TestData.Account();
        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _accounts.Setup(a => a.UpdateBankAccountAsync(account, It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        Assert.ThrowsAsync<ConcurrencyConflictException>(() => _service.DepositAsync(new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 1m }));
    }

    [Test]
    public async Task GetAccountTransactionSummaryAsync_ComputesTotalsAndStartingBalanceFromTheWholeLedger()
    {
        // Ledger: open 100, deposit 50, withdraw 30, deposit 0.01, withdraw 0.01 -> balance 120.
        var account = TestData.Account(initialDeposit: 100m);
        account.Deposit(50m, TestData.Now.AddDays(1));
        account.Withdraw(30m, TestData.Now.AddDays(2));
        account.Deposit(0.01m, TestData.Now.AddDays(3));
        account.Withdraw(0.01m, TestData.Now.AddDays(3));
        ArrangeLedger(account, page: 1, pageSize: 2);

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber, new LedgerPageRequestDto { Page = 1, PageSize = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(summary.AccountNumber, Is.EqualTo(account.AccountNumber));
            Assert.That(summary.TotalDeposits, Is.EqualTo(150.01m));
            Assert.That(summary.TotalWithdrawals, Is.EqualTo(30.01m));
            Assert.That(summary.TotalInterest, Is.Zero);
            Assert.That(summary.FinalBalance, Is.EqualTo(120m));
            Assert.That(summary.StartingBalance, Is.Zero, "every balance change is in the ledger, so the ledger starts from zero");
            Assert.That(summary.Transactions, Has.Count.EqualTo(2), "only the requested page is returned");
            Assert.That(summary.Transactions.Select(t => t.Amount), Is.EqualTo(new List<decimal> { 100m, 50m }));
            Assert.That(summary.Page, Is.EqualTo(new PageInfoDto(Number: 1, Size: 2, TotalItems: 5, TotalPages: 3)));
        });
    }

    [Test]
    public async Task GetAccountTransactionSummaryAsync_PassesTheRequestedPageToTheRepository()
    {
        var account = TestData.Account(initialDeposit: 100m);
        account.Deposit(1m, TestData.Now.AddDays(1));
        account.Deposit(2m, TestData.Now.AddDays(2));
        ArrangeLedger(account, page: 2, pageSize: 2);

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber, new LedgerPageRequestDto { Page = 2, PageSize = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(summary.Transactions.Select(t => t.Amount), Is.EqualTo(new List<decimal> { 2m }));
            Assert.That(summary.Page, Is.EqualTo(new PageInfoDto(2, 2, 3, 2)));
        });
        _transactions.Verify(t => t.GetPageByAccountIdAsync(account.BankAccountId, 2, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAccountTransactionSummaryAsync_StartingBalanceExplainsLegacyAccountsWithoutOpeningTransaction()
    {
        // Accounts created before the opening deposit was recorded have a balance not covered by the ledger.
        var account = TestData.Account(initialDeposit: 100m);
        account.Deposit(20m, TestData.Now.AddDays(1));
        var legacyLedger = account.Transactions.Skip(1).ToList(); // drop the opening deposit

        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _transactions.Setup(t => t.GetTotalsByAccountIdAsync(account.BankAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(Totals(legacyLedger));
        _transactions.Setup(t => t.GetPageByAccountIdAsync(account.BankAccountId, 1, LedgerPageRequestDto.DefaultPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LedgerPage(legacyLedger, 1, LedgerPageRequestDto.DefaultPageSize, legacyLedger.Count));

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber, new LedgerPageRequestDto());

        Assert.Multiple(() =>
        {
            Assert.That(summary.Transactions, Has.Count.EqualTo(1));
            Assert.That(summary.StartingBalance, Is.EqualTo(100m));
            Assert.That(summary.FinalBalance, Is.EqualTo(120m));
        });
    }

    [Test]
    public async Task TransferAsync_MovesMoneyAndSavesBothAccountsInOneCall()
    {
        var source = ArrangeAccount(100m, customerId: 1, accountNumber: "1000000001");
        var destination = ArrangeAccount(20m, customerId: 2, accountNumber: "1000000002");
        _accounts.Setup(a => a.SaveTransferAsync(source, destination, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.TransferAsync(new TransferRequestDto { SourceAccountNumber = "1000000001", DestinationAccountNumber = "1000000002", Amount = 30m });

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceAccountNumber, Is.EqualTo("1000000001"));
            Assert.That(result.DestinationAccountNumber, Is.EqualTo("1000000002"));
            Assert.That(result.Amount, Is.EqualTo(30m));
            Assert.That(result.SourceBalanceAfter, Is.EqualTo(70m));
            Assert.That(result.TransactionDate, Is.EqualTo(TestData.Now));
            Assert.That(destination.Balance, Is.EqualTo(50m));
        });
        _accounts.Verify(a => a.SaveTransferAsync(source, destination, It.IsAny<CancellationToken>()), Times.Once);
        _accounts.Verify(a => a.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void TransferAsync_ThrowsForbidden_WhenTheSourceBelongsToAnotherCustomer()
    {
        var source = ArrangeAccount(100m, customerId: 2, accountNumber: "1000000001");
        ArrangeAccount(20m, customerId: 1, accountNumber: "1000000002");

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _service.TransferAsync(new TransferRequestDto { SourceAccountNumber = "1000000001", DestinationAccountNumber = "1000000002", Amount = 30m }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(source.Balance, Is.EqualTo(100m));
        });
        _accounts.Verify(a => a.SaveTransferAsync(It.IsAny<BankAccount>(), It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void TransferAsync_ThrowsNotFound_WhenTheDestinationDoesNotExist()
    {
        ArrangeAccount(100m, customerId: 1, accountNumber: "1000000001");
        _accounts.Setup(a => a.GetByAccountNumberAsync("0000000000", It.IsAny<CancellationToken>())).ReturnsAsync((BankAccount?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _service.TransferAsync(new TransferRequestDto { SourceAccountNumber = "1000000001", DestinationAccountNumber = "0000000000", Amount = 30m }));

        Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.AccountNotFound));
        _accounts.Verify(a => a.SaveTransferAsync(It.IsAny<BankAccount>(), It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void TransferAsync_ThrowsSameAccountTransfer_WhenBothNumbersMatch()
    {
        var account = ArrangeAccount(100m, customerId: 1, accountNumber: "1000000001");

        var ex = Assert.ThrowsAsync<DomainException>(() => _service.TransferAsync(new TransferRequestDto { SourceAccountNumber = "1000000001", DestinationAccountNumber = "1000000001", Amount = 30m }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.SameAccountTransfer));
            Assert.That(account.Balance, Is.EqualTo(100m));
        });
    }

    [Test]
    public async Task GetAccountTransactionSummaryAsync_IncludesTransfersInTheTotalsAndStartingBalance()
    {
        // Ledger: open 100, transfer out 30, transfer in 5 -> balance 75.
        var account = TestData.Account(initialDeposit: 100m, accountNumber: "1000000001");
        var other = TestData.Account(customerId: 2, initialDeposit: 50m, accountNumber: "1000000002");
        account.TransferTo(other, 30m, TestData.Now.AddDays(1));
        other.TransferTo(account, 5m, TestData.Now.AddDays(2));
        ArrangeLedger(account, page: 1, pageSize: 10);

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber, new LedgerPageRequestDto { PageSize = 10 });

        Assert.Multiple(() =>
        {
            Assert.That(summary.TotalTransfersOut, Is.EqualTo(30m));
            Assert.That(summary.TotalTransfersIn, Is.EqualTo(5m));
            Assert.That(summary.FinalBalance, Is.EqualTo(75m));
            Assert.That(summary.StartingBalance, Is.Zero);
            Assert.That(summary.Transactions.Select(t => t.TransactionType), Is.EqualTo(new List<TransactionType> { TransactionType.Deposit, TransactionType.TransferOut, TransactionType.TransferIn }));
        });
    }

    /// <summary>Serves the account's in-memory ledger the way the repository would: totals over everything, one page of rows.</summary>
    private void ArrangeLedger(BankAccount account, int page, int pageSize)
    {
        var ledger = account.Transactions.ToList();
        var slice = ledger.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _transactions.Setup(t => t.GetTotalsByAccountIdAsync(account.BankAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(Totals(ledger));
        _transactions.Setup(t => t.GetPageByAccountIdAsync(account.BankAccountId, page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LedgerPage(slice, page, pageSize, ledger.Count));
    }

    private static LedgerTotals Totals(List<Transaction> ledger)
        => new(ledger.Count, ledger.GroupBy(t => t.TransactionType).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)));
}
