using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

/// <summary>EF Core context for the banking database (SQLite).</summary>
public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}
