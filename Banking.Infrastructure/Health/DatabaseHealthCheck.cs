using System.Data.Common;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Banking.Infrastructure.Health;

/// <summary>
/// Reports whether the banking database answers a trivial query. Opening the connection and
/// running <c>SELECT 1</c> catches a missing file, a locked or corrupt database and a wrong
/// connection string, which <c>/health/ready</c> turns into a <c>503</c>.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    /// <summary>Name of the check in the health report.</summary>
    public const string Name = "database";

    /// <summary>Tag of the checks that decide readiness (the process can serve requests).</summary>
    public const string ReadyTag = "ready";

    private readonly BankingDbContext _context;

    public DatabaseHealthCheck(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            return HealthCheckResult.Healthy("The database answers queries.");
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            // The exception is attached for logging; the response writer never exposes it.
            return HealthCheckResult.Unhealthy("The database cannot be reached.", ex);
        }
    }
}
