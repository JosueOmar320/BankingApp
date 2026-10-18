using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Banking.API.Health;

/// <summary>
/// Writes a health report as JSON: the overall status plus one entry per check with its status,
/// description and duration. Exceptions attached to a failed check are logged by the health
/// system but never returned.
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
    };

    /// <summary>Writes <paramref name="report"/> to the response (for <c>HealthCheckOptions.ResponseWriter</c>).</summary>
    public static Task WriteAsync(HttpContext httpContext, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(report);

        var body = new HealthResponse(
            report.Status,
            report.TotalDuration,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntry(entry.Value.Status, entry.Value.Description, entry.Value.Duration),
                StringComparer.Ordinal));

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(body, Json), httpContext.RequestAborted);
    }

    /// <summary>The body of a health endpoint.</summary>
    /// <param name="Status">Overall status: the worst of the checks, <c>Healthy</c> when there are none.</param>
    /// <param name="TotalDuration">Time spent running every check.</param>
    /// <param name="Checks">One entry per check, by name.</param>
    public sealed record HealthResponse(HealthStatus Status, TimeSpan TotalDuration, IReadOnlyDictionary<string, HealthCheckEntry> Checks);

    /// <summary>One check of the report.</summary>
    /// <param name="Status">Result of the check.</param>
    /// <param name="Description">What the check verified or why it failed.</param>
    /// <param name="Duration">Time the check took.</param>
    public sealed record HealthCheckEntry(HealthStatus Status, string? Description, TimeSpan Duration);
}
