using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.API.Middleware;

namespace Banking.Tests.API.Integration;

/// <summary>
/// Every response carries an <c>X-Correlation-Id</c> (the client's when well-formed, a fresh one
/// otherwise) and every problem response repeats it, so an error can be found in the logs.
/// </summary>
public class CorrelationIdTests : ApiTestBase
{
    [Test]
    public async Task Response_EchoesAWellFormedClientCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "order-42.retry_1");

        using var response = await Client.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), Is.EqualTo("order-42.retry_1"));
        });
    }

    [Test]
    public async Task Response_GeneratesACorrelationId_WhenTheClientSendsNone()
    {
        using var first = await Client.GetAsync("/health");
        using var second = await Client.GetAsync("/health");

        var firstId = first.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        var secondId = second.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.Multiple(() =>
        {
            Assert.That(firstId, Has.Length.EqualTo(32).And.Match("^[0-9a-f]+$"));
            Assert.That(secondId, Is.Not.EqualTo(firstId));
        });
    }

    [TestCase("")]
    [TestCase("has spaces")]
    [TestCase("<script>")]
    public async Task Response_ReplacesAMalformedClientCorrelationId(string supplied)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, supplied);

        using var response = await Client.SendAsync(request);

        var echoed = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.That(echoed, Has.Length.EqualTo(32).And.Not.EqualTo(supplied));
    }

    [Test]
    public async Task Response_ReplacesAClientCorrelationIdLongerThanTheMaximum()
    {
        var tooLong = new string('a', CorrelationIdMiddleware.MaxLength + 1);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, tooLong);

        using var response = await Client.SendAsync(request);

        Assert.That(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), Is.Not.EqualTo(tooLong));
    }

    [Test]
    public async Task ProblemResponses_CarryTheCorrelationId()
    {
        await CreateCustomerAsync();
        using var notFound = new HttpRequestMessage(HttpMethod.Get, "/api/BankAccount/0000000000");
        notFound.Headers.Add(CorrelationIdMiddleware.HeaderName, "trace-404");

        using var response = await Client.SendAsync(notFound);
        UseToken(null);
        using var unauthorized = await Client.GetAsync("/api/BankAccount/0000000000");

        var notFoundProblem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var unauthorizedProblem = await unauthorized.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(notFoundProblem.GetProperty(CorrelationIdMiddleware.ProblemExtension).GetString(), Is.EqualTo("trace-404"));
            Assert.That(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), Is.EqualTo("trace-404"));
            Assert.That(unauthorized.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(
                unauthorizedProblem.GetProperty(CorrelationIdMiddleware.ProblemExtension).GetString(),
                Is.EqualTo(unauthorized.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single()),
                "the 401 written by the bearer handler carries the same id as its header");
        });
    }

    [TestCase("abc", true)]
    [TestCase("a.b-c_d", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("a b", false)]
    public void IsWellFormed_AcceptsOnlySafeIds(string? candidate, bool expected)
        => Assert.That(CorrelationIdMiddleware.IsWellFormed(candidate), Is.EqualTo(expected));
}
