using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Infrastructure.Repositories;

public class BankAccountRepositoryTests
{
    private SqliteTestDatabase _database = null!;
    private int _customerId;

    [SetUp]
    public async Task SetUp()
    {
        _database = new SqliteTestDatabase();
        await using var context = _database.CreateContext();
        var customer = await new CustomerRepository(context).AddCustomerAsync(TestData.Customer());
        _customerId = customer.CustomerId;
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task AddBankAccountAsync_SavesAccountAndOpeningTransactionTogether()
    {
        await using var context = _database.CreateContext();
        var repository = new BankAccountRepository(context);

        var saved = await repository.AddBankAccountAsync(TestData.Account(_customerId, 100m));

        await using var verification = _database.CreateContext();
        var transactions = await verification.Transactions.Where(t => t.BankAccountId == saved.BankAccountId).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved.BankAccountId, Is.GreaterThan(0));
            Assert.That(transactions, Has.Count.EqualTo(1));
            Assert.That(transactions[0].Amount, Is.EqualTo(100m));
            Assert.That(transactions[0].BalanceAfter, Is.EqualTo(100m));
        });
    }

    [Test]
    public async Task AddBankAccountAsync_RejectsDuplicateAccountNumber()
    {
        await using var context = _database.CreateContext();
        var repository = new BankAccountRepository(context);
        await repository.AddBankAccountAsync(TestData.Account(_customerId, accountNumber: "1111111111"));

        Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.AddBankAccountAsync(TestData.Account(_customerId, accountNumber: "1111111111")));
    }

    [Test]
    public async Task AddBankAccountAsync_RejectsUnknownCustomer()
    {
        await using var context = _database.CreateContext();
        var repository = new BankAccountRepository(context);

        Assert.ThrowsAsync<DbUpdateException>(() => repository.AddBankAccountAsync(TestData.Account(customerId: 999)));
    }

    [Test]
    public async Task UpdateBankAccountAsync_SavesNewBalanceAndTransactionsAtomically()
    {
        await using (var setup = _database.CreateContext())
        {
            await new BankAccountRepository(setup).AddBankAccountAsync(TestData.Account(_customerId, 100m));
        }

        await using var context = _database.CreateContext();
        var repository = new BankAccountRepository(context);
        var account = await repository.GetByAccountNumberAsync("1234567890");
        Assert.That(account, Is.Not.Null);

        account!.Deposit(25m, TestData.Now);
        account.Withdraw(5m, TestData.Now);
        await repository.UpdateBankAccountAsync(account);

        await using var verification = _database.CreateContext();
        var stored = await verification.BankAccounts.Include(a => a.Transactions).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Balance, Is.EqualTo(120m));
            Assert.That(stored.Version, Is.EqualTo(3));
            Assert.That(stored.Transactions, Has.Count.EqualTo(3));
            Assert.That(stored.Transactions.Max(t => t.BalanceAfter), Is.EqualTo(125m));
        });
    }

    [Test]
    public async Task UpdateBankAccountAsync_DetectsConcurrentModification()
    {
        await using (var setup = _database.CreateContext())
        {
            await new BankAccountRepository(setup).AddBankAccountAsync(TestData.Account(_customerId, 100m));
        }

        // Two requests load the same account at the same time.
        await using var first = _database.CreateContext();
        await using var second = _database.CreateContext();
        var firstRepository = new BankAccountRepository(first);
        var secondRepository = new BankAccountRepository(second);
        var firstCopy = (await firstRepository.GetByAccountNumberAsync("1234567890"))!;
        var secondCopy = (await secondRepository.GetByAccountNumberAsync("1234567890"))!;

        // The first one wins...
        firstCopy.Withdraw(100m, TestData.Now);
        await firstRepository.UpdateBankAccountAsync(firstCopy);

        // ...and the second one, which still believes the balance is 100, must be rejected
        // instead of overdrawing the account.
        secondCopy.Withdraw(100m, TestData.Now);
        Assert.ThrowsAsync<ConcurrencyConflictException>(() => secondRepository.UpdateBankAccountAsync(secondCopy));

        await using var verification = _database.CreateContext();
        var stored = await verification.BankAccounts.SingleAsync();
        var transactionCount = await verification.Transactions.CountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stored.Balance, Is.Zero);
            Assert.That(stored.Version, Is.EqualTo(2));
            Assert.That(transactionCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetBalanceByAccountNumberAsync_ReturnsBalanceOrNull()
    {
        await using var context = _database.CreateContext();
        var repository = new BankAccountRepository(context);
        await repository.AddBankAccountAsync(TestData.Account(_customerId, 321m));

        var knownBalance = await repository.GetBalanceByAccountNumberAsync("1234567890");
        var unknownBalance = await repository.GetBalanceByAccountNumberAsync("0000000000");
        var knownExists = await repository.ExistsByNumberAsync("1234567890");
        var unknownExists = await repository.ExistsByNumberAsync("0000000000");

        Assert.Multiple(() =>
        {
            Assert.That(knownBalance, Is.EqualTo(321m));
            Assert.That(unknownBalance, Is.Null);
            Assert.That(knownExists, Is.True);
            Assert.That(unknownExists, Is.False);
        });
    }
}
