using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

/// <summary>
/// Inserts a small, deterministic data set so the API can be tried right after cloning.
/// Only runs when the database has no customers, so it never touches real data.
/// </summary>
public static class DevelopmentDataSeeder
{
    public const string AnaAccountNumber = "1000000001";
    public const string LuisAccountNumber = "1000000002";

    public static async Task SeedAsync(BankingDbContext context, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (await context.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var ana = Customer.Create("Ana", "Martínez", new DateOnly(1990, 4, 12), "001-120490-0001A", Gender.F, 1800m, now);
        var luis = Customer.Create("Luis", "Hernández", new DateOnly(1985, 11, 3), "001-031185-0002B", Gender.M, 2500m, now);
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
