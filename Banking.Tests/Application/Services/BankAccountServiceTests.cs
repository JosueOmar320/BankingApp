using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
using Banking.Domain.Rules;
using Banking.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banking.Tests.Application.Services;

public class BankAccountServiceTests
{
    private Mock<IBankAccountRepository> _accounts = null!;
    private Mock<ICustomerRepository> _customers = null!;
    private BankAccountService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IBankAccountRepository>(MockBehavior.Strict);
        _customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _service = new BankAccountService(_accounts.Object, _customers.Object, new FixedTimeProvider(), NullLogger<BankAccountService>.Instance);
    }

    [Test]
    public async Task CreateBankAccountAsync_OpensAccountWithGeneratedNumberAndOpeningDeposit()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());
        _accounts.Setup(a => a.ExistsByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        BankAccount? captured = null;
        _accounts
            .Setup(a => a.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
            .Callback<BankAccount, CancellationToken>((acc, _) => captured = acc)
            .ReturnsAsync((BankAccount acc, CancellationToken _) => acc);

        var result = await _service.CreateBankAccountAsync(new CreateBankAccountDto { CustomerId = 1, InitialDeposit = 250m });

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.AccountNumber, Has.Length.EqualTo(AccountRules.AccountNumberLength).And.Match("^[1-9][0-9]+$"));
            Assert.That(result.Balance, Is.EqualTo(250m));
            Assert.That(result.InterestRate, Is.EqualTo(AccountRules.InterestRate));
            Assert.That(result.CreatedAt, Is.EqualTo(TestData.Now));
            Assert.That(captured!.Transactions, Has.Count.EqualTo(1), "the opening deposit is part of the ledger");
        });
    }

    [Test]
    public async Task CreateBankAccountAsync_RetriesWhenGeneratedNumberIsTaken()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());
        _accounts
            .SetupSequence(a => a.ExistsByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _accounts
            .Setup(a => a.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankAccount acc, CancellationToken _) => acc);

        await _service.CreateBankAccountAsync(new CreateBankAccountDto { CustomerId = 1, InitialDeposit = 50m });

        _accounts.Verify(a => a.ExistsByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public void CreateBankAccountAsync_ThrowsNotFoundWhenCustomerMissing()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.CreateBankAccountAsync(new CreateBankAccountDto { CustomerId = 99, InitialDeposit = 100m }));
        _accounts.Verify(a => a.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateBankAccountAsync_RejectsDepositBelowMinimumBeforeSaving()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());
        _accounts.Setup(a => a.ExistsByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.ThrowsAsync<DomainException>(() => _service.CreateBankAccountAsync(new CreateBankAccountDto { CustomerId = 1, InitialDeposit = 9.99m }));
        _accounts.Verify(a => a.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetBalanceByAccountNumberAsync_ReturnsBalanceDto()
    {
        _accounts.Setup(a => a.GetBalanceByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(500m);

        var result = await _service.GetBalanceByAccountNumberAsync("123");

        Assert.Multiple(() =>
        {
            Assert.That(result.AccountNumber, Is.EqualTo("123"));
            Assert.That(result.Balance, Is.EqualTo(500m));
        });
    }

    [Test]
    public void GetBalanceByAccountNumberAsync_ThrowsNotFound()
    {
        _accounts.Setup(a => a.GetBalanceByAccountNumberAsync("999", It.IsAny<CancellationToken>())).ReturnsAsync((decimal?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.GetBalanceByAccountNumberAsync("999"));
    }

    [Test]
    public async Task GetByAccountNumberAsync_ReturnsAccountDto()
    {
        _accounts.Setup(a => a.GetByAccountNumberAsync("1234567890", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Account());

        var result = await _service.GetByAccountNumberAsync("1234567890");

        Assert.Multiple(() =>
        {
            Assert.That(result.AccountNumber, Is.EqualTo("1234567890"));
            Assert.That(result.Balance, Is.EqualTo(100m));
        });
    }
}
