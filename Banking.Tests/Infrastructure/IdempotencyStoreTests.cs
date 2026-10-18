using Banking.Infrastructure.Idempotency;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Infrastructure;

/// <summary>
/// The store on real SQLite: a key can be reserved once per customer, the same key of another
/// customer is independent, a completed key replays its response, a released key can be reused and
/// the cleanup deletes by age.
/// </summary>
public class IdempotencyStoreTests
{
    private const string Hash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    private SqliteTestDatabase _database = null!;
    private IdempotencyStore _store = null!;
    private int _ana;
    private int _luis;

    [SetUp]
    public async Task SetUp()
    {
        _database = new SqliteTestDatabase();
        _store = new IdempotencyStore(_database.Options);

        await using var context = _database.CreateContext();
        var customers = new CustomerRepository(context);
        _ana = (await customers.AddCustomerAsync(TestData.Customer())).CustomerId;
        _luis = (await customers.AddCustomerAsync(TestData.Customer(documentNumber: "001-020290-0002B", firstName: "Luis"))).CustomerId;
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task TryReserveAsync_ReservesANewKey_AndReturnsThePendingRecordOnTheSecondAttempt()
    {
        var first = await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now);
        var second = await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now.AddSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Null, "the first caller owns the key");
            Assert.That(second, Is.Not.Null);
            Assert.That(second!.IsCompleted, Is.False);
            Assert.That(second.RequestHash, Is.EqualTo(Hash));
            Assert.That(second.CreatedAt, Is.EqualTo(TestData.Now), "the record keeps the first request's timestamp");
            Assert.That(second.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public async Task TryReserveAsync_KeepsKeysOfDifferentCustomersApart()
    {
        var ana = await _store.TryReserveAsync(_ana, "shared-key", Hash, TestData.Now);
        var luis = await _store.TryReserveAsync(_luis, "shared-key", Hash, TestData.Now);

        Assert.Multiple(() =>
        {
            Assert.That(ana, Is.Null);
            Assert.That(luis, Is.Null, "the same key of another customer is a different operation");
        });
    }

    [Test]
    public async Task CompleteAsync_StoresTheResponse_ThatTheNextReservationReturns()
    {
        await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now);

        await _store.CompleteAsync(_ana, "k-1", 200, "application/json; charset=utf-8", """{"transactionId":7}""");
        var replay = await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now.AddMinutes(1));

        Assert.That(replay, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(replay!.IsCompleted, Is.True);
            Assert.That(replay.ResponseStatusCode, Is.EqualTo(200));
            Assert.That(replay.ResponseContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(replay.ResponseBody, Is.EqualTo("""{"transactionId":7}"""));
        });
    }

    [Test]
    public async Task ReleaseAsync_DeletesTheKey_SoItCanBeReservedAgain()
    {
        await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now);

        var released = await _store.ReleaseAsync(_ana, "k-1");
        var releasedAgain = await _store.ReleaseAsync(_ana, "k-1");
        var reserved = await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now);

        Assert.Multiple(() =>
        {
            Assert.That(released, Is.True);
            Assert.That(releasedAgain, Is.False);
            Assert.That(reserved, Is.Null);
        });
    }

    [Test]
    public async Task RemoveOlderThanAsync_DeletesOnlyTheKeysFirstSeenBeforeTheThreshold()
    {
        await _store.TryReserveAsync(_ana, "old", Hash, TestData.Now.AddHours(-25));
        await _store.TryReserveAsync(_ana, "recent", Hash, TestData.Now.AddHours(-1));
        await _store.TryReserveAsync(_luis, "old-too", Hash, TestData.Now.AddHours(-30));

        var removed = await _store.RemoveOlderThanAsync(TestData.Now.AddHours(-24));

        await using var context = _database.CreateContext();
        var remaining = await context.IdempotencyKeys.Select(k => k.Key).ToListAsync();
        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(remaining, Is.EqualTo(new List<string> { "recent" }));
        });
    }

    [Test]
    public async Task IdempotencyKeys_GoWithTheirCustomer()
    {
        await _store.TryReserveAsync(_ana, "k-1", Hash, TestData.Now);

        await using var context = _database.CreateContext();
        context.Customers.Remove(await context.Customers.SingleAsync(c => c.CustomerId == _ana));
        await context.SaveChangesAsync();

        Assert.That(await context.IdempotencyKeys.CountAsync(), Is.Zero, "keys are not financial records: they are deleted with the customer");
    }
}
