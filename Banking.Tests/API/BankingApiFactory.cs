using Banking.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Banking.Tests.API;

/// <summary>
/// Hosts the real API in-process on top of a private, shared-cache SQLite in-memory database.
/// The database lives as long as <see cref="_keepAlive"/> stays open, so migrations applied at
/// startup are visible to every request of the fixture.
/// </summary>
public sealed class BankingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:banking-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly string _environment;
    private readonly SqliteConnection _keepAlive;

    /// <param name="environment">Host environment; "Development" additionally enables Swagger.</param>
    public BankingApiFactory(string environment = "Testing")
    {
        _environment = environment;
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _connectionString);
        builder.UseSetting("Database:SeedSampleData", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
