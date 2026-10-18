using Banking.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Banking.Tests.Support;

/// <summary>
/// A private SQLite in-memory database that lives as long as this object. Using the real provider
/// (instead of the InMemory provider) means unique indexes, foreign keys and concurrency tokens
/// behave exactly as in production.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Options bound to this database, for components that open their own contexts.</summary>
    public DbContextOptions<BankingDbContext> Options { get; }

    public BankingDbContext CreateContext() => new(Options);

    public void Dispose() => _connection.Dispose();
}
