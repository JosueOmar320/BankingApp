using Banking.Infrastructure.Health;
using Banking.Infrastructure.Persistence;
using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Banking.Tests.Infrastructure;

/// <summary>The database check is healthy when SQLite answers and unhealthy when the file cannot be opened; it never throws.</summary>
public class DatabaseHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_IsHealthy_WhenTheDatabaseAnswers()
    {
        using var database = new SqliteTestDatabase();
        await using var context = database.CreateContext();

        var result = await new DatabaseHealthCheck(context).CheckHealthAsync(new HealthCheckContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public async Task CheckHealthAsync_IsUnhealthy_WhenTheDatabaseFileCannotBeOpened()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"banking-missing-{Guid.NewGuid():N}", "banking.db");
        var options = new DbContextOptionsBuilder<BankingDbContext>().UseSqlite($"Data Source={missingDirectory}").Options;
        await using var context = new BankingDbContext(options);

        var result = await new DatabaseHealthCheck(context).CheckHealthAsync(new HealthCheckContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
            Assert.That(result.Description, Does.Not.Contain(missingDirectory), "the description is safe to return to clients");
            Assert.That(result.Exception, Is.Not.Null, "the cause is attached for the logs");
        });
    }
}
