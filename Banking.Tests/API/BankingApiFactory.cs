using Banking.API.Auth;
using Banking.API.RateLimiting;
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
    /// <summary>A signing key for tests: long enough for HS256 and not the committed development key.</summary>
    public const string TestSigningKey = "integration-tests-signing-key-0123456789abcdef";

    private readonly string _connectionString = $"Data Source=file:banking-tests-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly string _environment;
    private readonly string? _signingKey;
    private readonly IReadOnlyDictionary<string, string?> _settings;
    private readonly SqliteConnection _keepAlive;

    /// <param name="environment">Host environment; "Development" additionally enables Swagger.</param>
    /// <param name="signingKey">Value of <c>Jwt:SigningKey</c>; <see langword="null"/> leaves it unset.</param>
    /// <param name="settings">Extra configuration values applied last, for fixtures that need specific limits.</param>
    public BankingApiFactory(string environment = "Testing", string? signingKey = TestSigningKey, IReadOnlyDictionary<string, string?>? settings = null)
    {
        _environment = environment;
        _signingKey = signingKey;
        _settings = settings ?? new Dictionary<string, string?>();
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", _connectionString);
        builder.UseSetting("Database:SeedSampleData", "false");
        if (_signingKey is not null)
        {
            builder.UseSetting($"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}", _signingKey);
        }

        // Every test of a fixture shares one host and one (unknown) client address, so the production
        // limits would trip across unrelated tests; rate-limiting tests pass their own small limits.
        builder.UseSetting($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.Authentication)}:{nameof(RateLimitPolicyOptions.PermitLimit)}", "100000");
        builder.UseSetting($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.MoneyOperations)}:{nameof(RateLimitPolicyOptions.PermitLimit)}", "100000");

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }
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
