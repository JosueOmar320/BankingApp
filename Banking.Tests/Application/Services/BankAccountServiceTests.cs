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
    private FakeCurrentCustomer _caller = null!;
    private BankAccountService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IBankAccountRepository>(MockBehavior.Strict);
        _customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _caller = new FakeCurrentCustomer(1);
        _service = new BankAccountService(_accounts.Object, _customers.Object, _caller, new FixedTimeProvider(), NullLogger<BankAccountService>.Instance);
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
    public void CreateBankAccountAsync_ThrowsForbiddenWhenOpeningForAnotherCustomer()
    {
        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _service.CreateBankAccountAsync(new CreateBankAccountDto { CustomerId = 2, InitialDeposit = 100m }));

        Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.CustomerAccessDenied));
        _accounts.Verify(a => a.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateBankAccountAsync_ThrowsNotFoundWhenCustomerMissing()
    {
        _caller.CustomerId = 99;
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
        _accounts.Setup(a => a.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Account(initialDeposit: 500m, accountNumber: "123"));

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
        _accounts.Setup(a => a.GetByAccountNumberAsync("999", It.IsAny<CancellationToken>())).ReturnsAsync((BankAccount?)null);

        Assert.ThrowsAsync<NotFoundException>(() => _service.GetBalanceByAccountNumberAsync("999"));
    }

    [Test]
    public void GetBalanceAndAccount_ThrowForbiddenForAnotherCustomersAccount()
    {
        _accounts.Setup(a => a.GetByAccountNumberAsync("1234567890", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Account(customerId: 2));

        var balance = Assert.ThrowsAsync<ForbiddenException>(() => _service.GetBalanceByAccountNumberAsync("1234567890"));
        var account = Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByAccountNumberAsync("1234567890"));

        Assert.Multiple(() =>
        {
            Assert.That(balance!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
            Assert.That(balance.MessageArguments, Is.EqualTo(new object[] { "1234567890" }));
            Assert.That(account!.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied));
        });
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

    [Test]
    public async Task GetByCustomerIdAsync_ReturnsTheCustomersAccountsInRepositoryOrder()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());
        _accounts.Setup(a => a.GetByCustomerIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            TestData.Account(customerId: 1, initialDeposit: 100m, accountNumber: "1111111111"),
            TestData.Account(customerId: 1, initialDeposit: 250m, accountNumber: "2222222222"),
        ]);

        var result = await _service.GetByCustomerIdAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(account => account.AccountNumber), Is.EqualTo(new List<string> { "1111111111", "2222222222" }));
            Assert.That(result.Select(account => account.Balance), Is.EqualTo(new List<decimal> { 100m, 250m }));
            Assert.That(result, Has.All.Property(nameof(BankAccountResponseDto.CustomerId)).EqualTo(1));
        });
    }

    [Test]
    public async Task GetByCustomerIdAsync_ReturnsAnEmptyList_WhenTheCustomerHasNoAccounts()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());
        _accounts.Setup(a => a.GetByCustomerIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _service.GetByCustomerIdAsync(1);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetByCustomerIdAsync_ThrowsNotFound_WhenTheCustomerDoesNotExist()
    {
        _caller.CustomerId = 99;
        _customers.Setup(c => c.GetCustomerByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _service.GetByCustomerIdAsync(99));

        Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.CustomerNotFound));
        _accounts.Verify(a => a.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void GetByCustomerIdAsync_ThrowsForbidden_ForAnotherCustomer_WithoutListingTheirAccounts()
    {
        _customers.Setup(c => c.GetCustomerByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer(documentNumber: "001-020290-0002B"));

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _service.GetByCustomerIdAsync(2));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.CustomerAccessDenied));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { 2 }));
        });
        _accounts.Verify(a => a.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
