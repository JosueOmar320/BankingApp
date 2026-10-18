using System.Net;
using System.Text;
using Banking.API.Idempotency;
using Banking.Infrastructure.Idempotency;
using Banking.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Banking.Tests.API.Idempotency;

/// <summary>
/// The filter's decisions against a mocked store: the paths the HTTP tests cannot reach without
/// a clock (a stale reservation is taken over, a fresh one answers 409) and the key format rule.
/// </summary>
public class IdempotencyFilterTests
{
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("""{"accountNumber":"1000000001","amount":10}""");

    private Mock<IIdempotencyStore> _store = null!;
    private IdempotencyFilter _filter = null!;
    private IdempotencyOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new Mock<IIdempotencyStore>(MockBehavior.Strict);
        _options = new IdempotencyOptions { PendingTimeoutSeconds = 60 };
        _filter = new IdempotencyFilter(_store.Object, new FakeCurrentCustomer(1), Options.Create(_options), new FixedTimeProvider(), NullLogger<IdempotencyFilter>.Instance);
    }

    [TestCase("8f0c6a1e-5b2d-4c3e-9f7a-1d2e3f4a5b6c", true)]
    [TestCase("a", true)]
    [TestCase("retry#1/deposit~2026", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("has space", false)]
    [TestCase("tab\there", false)]
    [TestCase("ñandú", false)]
    public void IsWellFormed_AcceptsVisibleAsciiUpToTheMaximumLength(string? key, bool expected)
        => Assert.That(IdempotencyFilter.IsWellFormed(key), Is.EqualTo(expected));

    [Test]
    public void IsWellFormed_RejectsKeysLongerThanTheMaximum()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IdempotencyFilter.IsWellFormed(new string('k', IdempotencyKey.MaxKeyLength)), Is.True);
            Assert.That(IdempotencyFilter.IsWellFormed(new string('k', IdempotencyKey.MaxKeyLength + 1)), Is.False);
        });
    }

    [Test]
    public async Task WithoutTheHeader_RunsTheOperation_WithoutTouchingTheStore()
    {
        var context = CreateContext(key: null);
        var executed = false;

        await _filter.OnResourceExecutionAsync(context, () => Next(context, () => executed = true));

        Assert.That(executed, Is.True);
        _store.VerifyNoOtherCalls();
    }

    [Test]
    public void WithAMalformedKey_Throws400_BeforeTouchingTheStore()
    {
        var context = CreateContext(key: new string('k', IdempotencyKey.MaxKeyLength + 1));

        var ex = Assert.ThrowsAsync<IdempotencyKeyException>(() => _filter.OnResourceExecutionAsync(context, () => Next(context)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(ex.Code, Is.EqualTo("IdempotencyKeyInvalid"));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { IdempotencyKey.MaxKeyLength }));
        });
        _store.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AStaleReservationForTheSameRequest_IsTakenOver_AndTheOperationRuns()
    {
        var staleSince = TestData.Now - _options.PendingTimeout - TimeSpan.FromSeconds(1);
        var context = CreateContext(key: "k-1");
        var executed = false;

        var reservations = 0;
        _store
            .Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int customerId, string key, string hash, DateTime _, CancellationToken _)
                => reservations++ == 0 ? new IdempotencyKey(customerId, key, hash, staleSince) : null);
        _store.Setup(s => s.ReleaseAsync(1, "k-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _store.Setup(s => s.CompleteAsync(1, "k-1", 200, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _filter.OnResourceExecutionAsync(context, () => Next(context, () => executed = true));

        Assert.That(executed, Is.True);
        _store.Verify(s => s.ReleaseAsync(1, "k-1", It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _store.Verify(s => s.CompleteAsync(1, "k-1", 200, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AFreshReservationForTheSameRequest_Throws409_WithoutRunningTheOperation()
    {
        var context = CreateContext(key: "k-1");
        var executed = false;

        _store
            .Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int customerId, string key, string hash, DateTime _, CancellationToken _) => new IdempotencyKey(customerId, key, hash, TestData.Now.AddSeconds(-5)));

        var ex = Assert.ThrowsAsync<IdempotencyKeyException>(() => _filter.OnResourceExecutionAsync(context, () => Next(context, () => executed = true)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(ex.Code, Is.EqualTo("IdempotencyKeyInProgress"));
            Assert.That(executed, Is.False);
        });
        _store.Verify(s => s.ReleaseAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void AStaleReservationForADifferentRequest_IsStillRejectedWith422()
    {
        var staleSince = TestData.Now - _options.PendingTimeout - TimeSpan.FromMinutes(5);
        var context = CreateContext(key: "k-1");

        _store
            .Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyKey(1, "k-1", new string('0', IdempotencyKey.RequestHashLength), staleSince));

        var ex = Assert.ThrowsAsync<IdempotencyKeyException>(() => _filter.OnResourceExecutionAsync(context, () => Next(context)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.StatusCode, Is.EqualTo(StatusCodes.Status422UnprocessableEntity));
            Assert.That(ex.Code, Is.EqualTo("IdempotencyKeyReused"));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { "k-1" }));
        });
        _store.Verify(s => s.ReleaseAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AnErrorResponse_ReleasesTheKey_AndIsStillSentToTheClient()
    {
        var context = CreateContext(key: "k-1");
        _store.Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>())).ReturnsAsync((IdempotencyKey?)null);
        _store.Setup(s => s.ReleaseAsync(1, "k-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _filter.OnResourceExecutionAsync(context, () => Next(context, () =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.HttpContext.Response.Body.Write("problem"u8);
        }));

        var sent = (MemoryStream)context.HttpContext.Response.Body;
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(sent.ToArray()), Is.EqualTo("problem"));
            Assert.That(context.HttpContext.Response.StatusCode, Is.EqualTo(400));
        });
        _store.Verify(s => s.ReleaseAsync(1, "k-1", It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.CompleteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ASuccessfulResponse_IsStoredWithItsExactBody_AndSentToTheClient()
    {
        var context = CreateContext(key: "k-1");
        string? storedBody = null;
        _store.Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>())).ReturnsAsync((IdempotencyKey?)null);
        _store
            .Setup(s => s.CompleteAsync(1, "k-1", 200, "application/json; charset=utf-8", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, int, string, string, CancellationToken>((_, _, _, _, body, _) => storedBody = body)
            .Returns(Task.CompletedTask);

        await _filter.OnResourceExecutionAsync(context, () => Next(context, () =>
        {
            context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
            context.HttpContext.Response.Body.Write("""{"transactionId":7}"""u8);
        }));

        var sent = (MemoryStream)context.HttpContext.Response.Body;
        Assert.Multiple(() =>
        {
            Assert.That(storedBody, Is.EqualTo("""{"transactionId":7}"""));
            Assert.That(Encoding.UTF8.GetString(sent.ToArray()), Is.EqualTo(storedBody));
        });
        _store.Verify(s => s.ReleaseAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ACompletedKey_ReplaysTheStoredResponse_WithoutRunningTheOperation()
    {
        var context = CreateContext(key: "k-1");
        var executed = false;
        _store
            .Setup(s => s.TryReserveAsync(1, "k-1", It.IsAny<string>(), TestData.Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int customerId, string key, string hash, DateTime _, CancellationToken _) =>
            {
                var record = new IdempotencyKey(customerId, key, hash, TestData.Now.AddMinutes(-1));
                record.Complete(200, "application/json; charset=utf-8", """{"transactionId":7}""");
                return record;
            });

        await _filter.OnResourceExecutionAsync(context, () => Next(context, () => executed = true));

        Assert.Multiple(() =>
        {
            Assert.That(executed, Is.False);
            Assert.That(context.Result, Is.InstanceOf<StoredResponseResult>());
        });
    }

    private static ResourceExecutingContext CreateContext(string? key)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/Transaction/Deposit";
        httpContext.Request.Body = new MemoryStream(Body);
        httpContext.Request.ContentLength = Body.Length;
        httpContext.Response.Body = new MemoryStream();
        if (key is not null)
        {
            httpContext.Request.Headers[IdempotencyFilter.HeaderName] = key;
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        return new ResourceExecutingContext(actionContext, [], []);
    }

    /// <summary>Stands in for the rest of the pipeline: runs <paramref name="action"/> against the response and reports success.</summary>
    private static Task<ResourceExecutedContext> Next(ResourceExecutingContext context, Action? action = null)
    {
        action?.Invoke();
        return Task.FromResult(new ResourceExecutedContext(context, context.Filters));
    }
}
