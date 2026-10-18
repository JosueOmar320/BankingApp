using Banking.Domain.Interfaces;
using Banking.Infrastructure.Health;
using Banking.Infrastructure.Idempotency;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Repositories;
using Banking.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Banking.Infrastructure;

/// <summary>
/// Registers the SQLite <see cref="BankingDbContext"/>, the repositories, the password hasher, the
/// database health check and the idempotency-key store with its cleanup service.
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is missing. Add it under 'ConnectionStrings' in appsettings.json.");
        }

        services.AddDbContext<BankingDbContext>((provider, options) =>
        {
            var environment = provider.GetRequiredService<IHostEnvironment>();
            options.UseSqlite(ResolveDataSource(connectionString, environment.ContentRootPath));
        });

        services.AddRepositories();

        // Readiness: the database answers a query. /health/ready selects checks by this tag.
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name, tags: [DatabaseHealthCheck.ReadyTag]);

        // Idempotency-Key storage and purge; the host binds and validates IdempotencyOptions.
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddHostedService<IdempotencyKeyCleanupService>();

        return services;
    }

    /// <summary>
    /// Registers the repositories and the password hasher; the caller configures
    /// <see cref="BankingDbContext"/> itself (tests).
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBankAccountRepository, BankAccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        return services;
    }

    /// <summary>
    /// Makes a relative SQLite file path relative to the content root instead of the process
    /// working directory, so the same database is used whether the API is started from the IDE,
    /// from <c>dotnet run</c> or from a published folder.
    /// </summary>
    internal static string ResolveDataSource(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.StartsWith(':')
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, dataSource.Replace('\\', '/')));
        return builder.ToString();
    }
}
