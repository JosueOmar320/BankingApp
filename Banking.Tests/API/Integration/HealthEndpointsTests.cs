using System.Net;
using System.Net.Http.Json;
using Banking.API.Health;
using Banking.Infrastructure.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Banking.Tests.API.Integration;

/// <summary>
/// <c>/health</c> reports every check (the database included) as JSON, <c>/health/live</c> only
/// says the process answers and <c>/health/ready</c> runs the readiness checks. All three are public.
/// </summary>
public class HealthEndpointsTests : ApiTestBase
{
    [SetUp]
    public void Anonymous() => UseToken(null);

    [Test]
    public async Task Health_ReportsTheDatabaseCheckHealthy_AsJson()
    {
        using var response = await Client.GetAsync(HealthEndpoints.All);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var report = await response.Content.ReadFromJsonAsync<HealthResponseWriter.HealthResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            Assert.That(report!.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(report.Checks.Keys, Is.EqualTo(new List<string> { DatabaseHealthCheck.Name }));
            Assert.That(report.Checks[DatabaseHealthCheck.Name].Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(report.Checks[DatabaseHealthCheck.Name].Description, Is.Not.Empty);
        });
    }

    [Test]
    public async Task HealthLive_Returns200WithoutRunningAnyCheck()
    {
        using var response = await Client.GetAsync(HealthEndpoints.Live);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var report = await response.Content.ReadFromJsonAsync<HealthResponseWriter.HealthResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(report!.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(report.Checks, Is.Empty);
        });
    }

    [Test]
    public async Task HealthReady_RunsTheDatabaseCheck()
    {
        using var response = await Client.GetAsync(HealthEndpoints.Ready);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var report = await response.Content.ReadFromJsonAsync<HealthResponseWriter.HealthResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(report!.Status, Is.EqualTo(HealthStatus.Healthy));
            Assert.That(report.Checks.Keys, Is.EqualTo(new List<string> { DatabaseHealthCheck.Name }));
        });
    }

    [Test]
    public async Task HealthResponses_NeverIncludeExceptionDetails()
    {
        using var response = await Client.GetAsync(HealthEndpoints.All);

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Contain("exception").IgnoreCase);
    }
}
