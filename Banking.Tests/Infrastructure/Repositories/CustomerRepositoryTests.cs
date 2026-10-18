using Banking.Domain.Exceptions;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;

namespace Banking.Tests.Infrastructure.Repositories;

public class CustomerRepositoryTests
{
    private SqliteTestDatabase _database = null!;

    [SetUp]
    public void SetUp() => _database = new SqliteTestDatabase();

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task AddCustomerAsync_PersistsAndAssignsId()
    {
        await using var context = _database.CreateContext();
        var repository = new CustomerRepository(context);

        var saved = await repository.AddCustomerAsync(TestData.Customer());

        Assert.That(saved.CustomerId, Is.GreaterThan(0));

        await using var verification = _database.CreateContext();
        var stored = await new CustomerRepository(verification).GetCustomerByIdAsync(saved.CustomerId);
        Assert.That(stored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stored!.FirstName, Is.EqualTo("Ana"));
            Assert.That(stored.DocumentNumber, Is.EqualTo("001-010190-0001A"));
            Assert.That(stored.Gender, Is.EqualTo(Banking.Domain.Enums.Gender.F));
            Assert.That(stored.CreatedAt, Is.EqualTo(TestData.Now));
            Assert.That(stored.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc), "timestamps must round-trip as UTC");
        });
    }

    [Test]
    public async Task AddCustomerAsync_RejectsDuplicateDocumentNumber()
    {
        await using var context = _database.CreateContext();
        var repository = new CustomerRepository(context);
        await repository.AddCustomerAsync(TestData.Customer("DOC-1", "Ana"));

        Assert.ThrowsAsync<DuplicateDocumentNumberException>(() => repository.AddCustomerAsync(TestData.Customer("DOC-1", "Luis")));

        // The failed entity must not poison the context: a different document still saves.
        var third = await repository.AddCustomerAsync(TestData.Customer("DOC-2", "Luis"));
        Assert.That(third.CustomerId, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetCustomerByIdAsync_ReturnsNullForUnknownId()
    {
        await using var context = _database.CreateContext();

        var result = await new CustomerRepository(context).GetCustomerByIdAsync(404);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCustomerByDocumentNumberAsync_ReturnsTheCustomerWithItsPasswordHash()
    {
        await using var context = _database.CreateContext();
        var saved = await new CustomerRepository(context).AddCustomerAsync(TestData.Customer("DOC-LOGIN"));

        await using var verification = _database.CreateContext();
        var repository = new CustomerRepository(verification);
        var found = await repository.GetCustomerByDocumentNumberAsync("DOC-LOGIN");
        var missing = await repository.GetCustomerByDocumentNumberAsync("DOC-NOBODY");

        Assert.Multiple(() =>
        {
            Assert.That(found?.CustomerId, Is.EqualTo(saved.CustomerId));
            Assert.That(found!.PasswordHash, Is.EqualTo(saved.PasswordHash), "the hash round-trips through the database");
            Assert.That(found.VerifyPassword(TestData.Password, TestData.Hasher), Is.True);
            Assert.That(missing, Is.Null);
        });
    }
}
