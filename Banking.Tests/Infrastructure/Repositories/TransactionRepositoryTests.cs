using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;

namespace Banking.Tests.Infrastructure.Repositories;

public class TransactionRepositoryTests
{
    private SqliteTestDatabase _database = null!;
    private int _targetId;

    [SetUp]
    public async Task SetUp()
    {
        _database = new SqliteTestDatabase();

        // Target ledger (by date): open 100, withdraw 5, deposit 10, interest 10.50; another account has its own rows.
        await using var setup = _database.CreateContext();
        var customer = await new CustomerRepository(setup).AddCustomerAsync(TestData.Customer());
        var accounts = new BankAccountRepository(setup);

        var target = TestData.Account(customer.CustomerId, 100m, "1000000001");
        target.Deposit(10m, TestData.Now.AddDays(2));
        target.Withdraw(5m, TestData.Now.AddDays(1));
        target.ApplyInterest(TestData.Now.AddDays(3));
        await accounts.AddBankAccountAsync(target);
        _targetId = target.BankAccountId;

        var other = TestData.Account(customer.CustomerId, 50m, "1000000002");
        other.Deposit(1m, TestData.Now.AddDays(3));
        await accounts.AddBankAccountAsync(other);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task GetPageByAccountIdAsync_ReturnsOnlyThatAccountOldestFirst()
    {
        await using var context = _database.CreateContext();

        var page = await new TransactionRepository(context).GetPageByAccountIdAsync(_targetId, page: 1, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(page.Transactions, Has.Count.EqualTo(4));
            Assert.That(page.Transactions.Select(t => t.TransactionDate), Is.Ordered);
            Assert.That(page.Transactions.Select(t => t.TransactionType), Is.EqualTo(new List<TransactionType> { TransactionType.Deposit, TransactionType.Withdrawal, TransactionType.Deposit, TransactionType.Interest }));
            Assert.That(page.Transactions.All(t => t.BankAccountId == _targetId), Is.True);
            Assert.That(page, Is.EqualTo(page with { Page = 1, PageSize = 10, TotalCount = 4 }));
            Assert.That(page.TotalPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetPageByAccountIdAsync_SlicesTheLedgerAndCountsTheWholeOfIt()
    {
        await using var context = _database.CreateContext();
        var repository = new TransactionRepository(context);

        var second = await repository.GetPageByAccountIdAsync(_targetId, page: 2, pageSize: 3);
        var beyond = await repository.GetPageByAccountIdAsync(_targetId, page: 3, pageSize: 3);

        Assert.Multiple(() =>
        {
            Assert.That(second.Transactions.Select(t => t.Amount), Is.EqualTo(new List<decimal> { 10.50m }), "the fourth transaction is the interest");
            Assert.That(second.TotalCount, Is.EqualTo(4));
            Assert.That(second.TotalPages, Is.EqualTo(2));
            Assert.That(beyond.Transactions, Is.Empty);
            Assert.That(beyond.TotalCount, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task GetPageByAccountIdAsync_ReturnsEmptyForUnknownAccount()
    {
        await using var context = _database.CreateContext();

        var page = await new TransactionRepository(context).GetPageByAccountIdAsync(404, 1, 10);

        Assert.Multiple(() =>
        {
            Assert.That(page.Transactions, Is.Empty);
            Assert.That(page.TotalCount, Is.Zero);
            Assert.That(page.TotalPages, Is.Zero);
        });
    }

    [TestCase(0, 10)]
    [TestCase(1, 0)]
    public void GetPageByAccountIdAsync_RejectsPageOrSizeBelowOne(int page, int pageSize)
    {
        using var context = _database.CreateContext();

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new TransactionRepository(context).GetPageByAccountIdAsync(_targetId, page, pageSize));
    }

    [Test]
    public async Task GetTotalsByAccountIdAsync_SumsEachTypeOverTheWholeLedgerWithMoneyPrecision()
    {
        await using var context = _database.CreateContext();

        var totals = await new TransactionRepository(context).GetTotalsByAccountIdAsync(_targetId);

        Assert.Multiple(() =>
        {
            Assert.That(totals.TransactionCount, Is.EqualTo(4));
            Assert.That(totals.Of(TransactionType.Deposit), Is.EqualTo(110m));
            Assert.That(totals.Of(TransactionType.Withdrawal), Is.EqualTo(5m));
            Assert.That(totals.Of(TransactionType.Interest), Is.EqualTo(10.50m));
        });
    }

    [Test]
    public async Task GetTotalsByAccountIdAsync_ReturnsEmptyTotalsForUnknownAccount()
    {
        await using var context = _database.CreateContext();

        var totals = await new TransactionRepository(context).GetTotalsByAccountIdAsync(404);

        Assert.Multiple(() =>
        {
            Assert.That(totals, Is.SameAs(LedgerTotals.Empty));
            Assert.That(totals.TransactionCount, Is.Zero);
            Assert.That(totals.Of(TransactionType.Deposit), Is.Zero);
        });
    }
}
