using Banking.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Banking.API.Health;

/// <summary>
/// The three public health endpoints:
/// <list type="bullet">
/// <item><c>/health</c>: every registered check (what <c>docker-compose</c> and the README use).</item>
/// <item><c>/health/live</c>: no checks; <c>200</c> as long as the process answers HTTP.</item>
/// <item><c>/health/ready</c>: the checks tagged <see cref="DatabaseHealthCheck.ReadyTag"/>, i.e. the database is reachable.</item>
/// </list>
/// All three answer with the JSON body of <see cref="HealthResponseWriter"/>; an unhealthy report is a <c>503</c>.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>Route of the full report.</summary>
    public const string All = "/health";

    /// <summary>Route of the liveness probe.</summary>
    public const string Live = "/health/live";

    /// <summary>Route of the readiness probe.</summary>
    public const string Ready = "/health/ready";

    /// <summary>Maps the three endpoints; all of them are anonymous.</summary>
    public static IEndpointRouteBuilder MapBankingHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHealthChecks(All, new HealthCheckOptions
        {
            ResponseWriter = HealthResponseWriter.WriteAsync,
        }).AllowAnonymous();

        endpoints.MapHealthChecks(Live, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponseWriter.WriteAsync,
        }).AllowAnonymous();

        endpoints.MapHealthChecks(Ready, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(DatabaseHealthCheck.ReadyTag),
            ResponseWriter = HealthResponseWriter.WriteAsync,
        }).AllowAnonymous();

        return endpoints;
    }
}
