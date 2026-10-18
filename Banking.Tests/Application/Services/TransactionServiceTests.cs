using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
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
            Assert.That(Assert.ThrowsAsync<ForbiddenException>(() => _service.GetAccountTransactionSummaryAsync(account.AccountNumber))!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(account.Balance, Is.EqualTo(100m));
        });
        _accounts.Verify(a => a.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private BankAccount ArrangeAccount(decimal initialDeposit = 100m, int customerId = 1)
    {
        var account = TestData.Account(customerId, initialDeposit);
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
            Assert.ThrowsAsync<NotFoundException>(() => _service.GetAccountTransactionSummaryAsync("999"));
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
    public async Task GetAccountTransactionSummaryAsync_ComputesTotalsAndStartingBalance()
    {
        // Ledger: open 100, deposit 50, withdraw 30, interest 12 -> balance 132.
        var account = TestData.Account(initialDeposit: 100m);
        account.Deposit(50m, TestData.Now.AddDays(1));
        account.Withdraw(30m, TestData.Now.AddDays(2));
        account.Deposit(0.01m, TestData.Now.AddDays(3));
        account.Withdraw(0.01m, TestData.Now.AddDays(3));
        var ledger = account.Transactions.ToList();

        // Simulate an account whose balance was reached through the ledger above.
        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _transactions.Setup(t => t.GetAllByAccountIdAsync(account.BankAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(ledger);

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber);

        Assert.Multiple(() =>
        {
            Assert.That(summary.AccountNumber, Is.EqualTo(account.AccountNumber));
            Assert.That(summary.Transactions, Has.Count.EqualTo(5));
            Assert.That(summary.TotalDeposits, Is.EqualTo(150.01m));
            Assert.That(summary.TotalWithdrawals, Is.EqualTo(30.01m));
            Assert.That(summary.TotalInterest, Is.Zero);
            Assert.That(summary.FinalBalance, Is.EqualTo(120m));
            Assert.That(summary.StartingBalance, Is.Zero, "every balance change is in the ledger, so the ledger starts from zero");
        });
    }

    [Test]
    public async Task GetAccountTransactionSummaryAsync_StartingBalanceExplainsLegacyAccountsWithoutOpeningTransaction()
    {
        // Accounts created before the opening deposit was recorded have a balance not covered by the ledger.
        var account = TestData.Account(initialDeposit: 100m);
        var legacyLedger = account.Transactions.Skip(1).ToList(); // drop the opening deposit
        account.Deposit(20m, TestData.Now.AddDays(1));
        legacyLedger = account.Transactions.Skip(1).ToList();

        _accounts.Setup(a => a.GetByAccountNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _transactions.Setup(t => t.GetAllByAccountIdAsync(account.BankAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(legacyLedger);

        var summary = await _service.GetAccountTransactionSummaryAsync(account.AccountNumber);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Transactions, Has.Count.EqualTo(1));
            Assert.That(summary.StartingBalance, Is.EqualTo(100m));
            Assert.That(summary.FinalBalance, Is.EqualTo(120m));
        });
    }
}
