using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Security;
using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Infrastructure;

public class DevelopmentDataSeederTests
{
    [Test]
    public async Task SeedAsync_CreatesSampleCustomersThatCanLogInWithTheDocumentedPassword()
    {
        using var database = new SqliteTestDatabase();
        var hasher = new IdentityPasswordHasher();

        await using (var context = database.CreateContext())
        {
            await DevelopmentDataSeeder.SeedAsync(context, new FixedTimeProvider(), hasher);
        }

        await using var verification = database.CreateContext();
        var customers = await verification.Customers.ToListAsync();
        var accounts = await verification.BankAccounts.ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(customers.Select(c => c.DocumentNumber), Is.EquivalentTo(new[] { DevelopmentDataSeeder.AnaDocumentNumber, DevelopmentDataSeeder.LuisDocumentNumber }));
            Assert.That(customers, Has.All.Matches<Banking.Domain.Entities.Customer>(c => c.VerifyPassword(DevelopmentDataSeeder.SamplePassword, hasher)));
            Assert.That(accounts.Select(a => a.AccountNumber), Is.EquivalentTo(new[] { DevelopmentDataSeeder.AnaAccountNumber, DevelopmentDataSeeder.LuisAccountNumber }));
        });
    }

    [Test]
    public async Task SeedAsync_IsIdempotent()
    {
        using var database = new SqliteTestDatabase();
        var hasher = new IdentityPasswordHasher();

        await using (var context = database.CreateContext())
        {
            await DevelopmentDataSeeder.SeedAsync(context, new FixedTimeProvider(), hasher);
        }

        await using (var context = database.CreateContext())
        {
            await DevelopmentDataSeeder.SeedAsync(context, new FixedTimeProvider(), hasher);
        }

        await using var verification = database.CreateContext();
        Assert.That(await verification.Customers.CountAsync(), Is.EqualTo(2));
    }
}
