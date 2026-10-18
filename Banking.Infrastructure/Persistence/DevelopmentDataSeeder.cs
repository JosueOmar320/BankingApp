using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

/// <summary>
/// Inserts a small, deterministic data set so the API can be tried right after cloning.
/// Only runs when the database has no customers, so it never touches real data.
/// Both sample customers log in with <see cref="SamplePassword"/>.
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string AnaDocumentNumber = "001-120490-0001A";
    public const string AnaAccountNumber = "1000000001";
    public const string LuisDocumentNumber = "001-031185-0002B";
    public const string LuisAccountNumber = "1000000002";

    /// <summary>Password of every seeded customer. Development only; the seeder never runs on real data.</summary>
    public const string SamplePassword = "Banking123!";

    public static async Task SeedAsync(BankingDbContext context, TimeProvider timeProvider, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        if (await context.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var ana = Customer.Create("Ana", "Martínez", new DateOnly(1990, 4, 12), AnaDocumentNumber, Gender.F, 1800m, SamplePassword, passwordHasher, now);
        var luis = Customer.Create("Luis", "Hernández", new DateOnly(1985, 11, 3), LuisDocumentNumber, Gender.M, 2500m, SamplePassword, passwordHasher, now);
        context.Customers.AddRange(ana, luis);
        await context.SaveChangesAsync(cancellationToken);

        var anaAccount = BankAccount.Open(AnaAccountNumber, ana.CustomerId, 500m, now.AddDays(-30));
        anaAccount.Deposit(250m, now.AddDays(-20));
        anaAccount.Withdraw(100m, now.AddDays(-10));
        anaAccount.ApplyInterest(now.AddDays(-1));

        var luisAccount = BankAccount.Open(LuisAccountNumber, luis.CustomerId, 1000m, now.AddDays(-15));
        luisAccount.Withdraw(200m, now.AddDays(-5));

        context.BankAccounts.AddRange(anaAccount, luisAccount);
        await context.SaveChangesAsync(cancellationToken);
    }
}
