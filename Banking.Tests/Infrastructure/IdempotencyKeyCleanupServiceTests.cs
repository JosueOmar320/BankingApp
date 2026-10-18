using Banking.Infrastructure.Idempotency;
using Banking.Infrastructure.Repositories;
using Banking.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Banking.Tests.Infrastructure;

/// <summary>One cleanup pass removes the keys older than the configured lifetime and survives a failing store.</summary>
public class IdempotencyKeyCleanupServiceTests
{
    private const string Hash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    [Test]
    public async Task CleanUpOnceAsync_RemovesKeysOlderThanTheLifetime()
    {
        using var database = new SqliteTestDatabase();
        var store = new IdempotencyStore(database.Options);
        int customerId;
        await using (var context = database.CreateContext())
        {
            customerId = (await new CustomerRepository(context).AddCustomerAsync(TestData.Customer())).CustomerId;
        }

        await store.TryReserveAsync(customerId, "expired", Hash, TestData.Now.AddHours(-24).AddSeconds(-1));
        await store.TryReserveAsync(customerId, "alive", Hash, TestData.Now.AddHours(-23));

        await using var services = new ServiceCollection().AddScoped<IIdempotencyStore>(_ => store).BuildServiceProvider();
        var service = new IdempotencyKeyCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdempotencyOptions { KeyLifetimeHours = 24 }),
            new FixedTimeProvider(),
            NullLogger<IdempotencyKeyCleanupService>.Instance);

        var removed = await service.CleanUpOnceAsync(CancellationToken.None);

        await using var verification = database.CreateContext();
        Assert.Multiple(async () =>
        {
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(await verification.IdempotencyKeys.Select(k => k.Key).ToListAsync(), Is.EqualTo(new List<string> { "alive" }));
        });
    }

    [Test]
    public async Task CleanUpOnceAsync_SwallowsStoreFailures_SoTheServiceKeepsRunning()
    {
        var failing = new Mock<IIdempotencyStore>(MockBehavior.Strict);
        failing
            .Setup(s => s.RemoveOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        await using var services = new ServiceCollection().AddScoped(_ => failing.Object).BuildServiceProvider();
        var service = new IdempotencyKeyCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdempotencyOptions()),
            new FixedTimeProvider(),
            NullLogger<IdempotencyKeyCleanupService>.Instance);

        var removed = await service.CleanUpOnceAsync(CancellationToken.None);

        Assert.That(removed, Is.Zero);
        failing.VerifyAll();
    }
}
