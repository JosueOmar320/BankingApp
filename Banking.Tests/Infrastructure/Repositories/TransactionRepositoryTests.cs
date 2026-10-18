using Banking.Domain.Enums;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;

namespace Banking.Tests.Infrastructure.Repositories;

public class TransactionRepositoryTests
{
    private SqliteTestDatabase _database = null!;

    [SetUp]
    public void SetUp() => _database = new SqliteTestDatabase();

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task GetAllByAccountIdAsync_ReturnsOnlyThatAccountOldestFirst()
    {
        int targetId;
        await using (var setup = _database.CreateContext())
        {
            var customer = await new CustomerRepository(setup).AddCustomerAsync(TestData.Customer());
            var accounts = new BankAccountRepository(setup);

            var target = TestData.Account(customer.CustomerId, 100m, "1000000001");
            target.Deposit(10m, TestData.Now.AddDays(2));
            target.Withdraw(5m, TestData.Now.AddDays(1));
            await accounts.AddBankAccountAsync(target);
            targetId = target.BankAccountId;

            var other = TestData.Account(customer.CustomerId, 50m, "1000000002");
            other.Deposit(1m, TestData.Now.AddDays(3));
            await accounts.AddBankAccountAsync(other);
        }

        await using var context = _database.CreateContext();
        var transactions = await new TransactionRepository(context).GetAllByAccountIdAsync(targetId);

        Assert.That(transactions, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(transactions.Select(t => t.TransactionDate), Is.Ordered);
            Assert.That(transactions[0].TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(transactions[1].TransactionType, Is.EqualTo(TransactionType.Withdrawal));
            Assert.That(transactions[2].TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(transactions.All(t => t.BankAccountId == targetId), Is.True);
        });
    }

    [Test]
    public async Task GetAllByAccountIdAsync_ReturnsEmptyForUnknownAccount()
    {
        await using var context = _database.CreateContext();

        var transactions = await new TransactionRepository(context).GetAllByAccountIdAsync(404);

        Assert.That(transactions, Is.Empty);
    }
}
