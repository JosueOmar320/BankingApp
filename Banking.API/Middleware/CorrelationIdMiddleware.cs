using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Features;

namespace Banking.API.Middleware;

/// <summary>
/// Gives every request a correlation id: the <c>X-Correlation-Id</c> header when the client sends a
/// well-formed one, a new one otherwise. The id is echoed in the response header, becomes the
/// request's <see cref="HttpContext.TraceIdentifier"/>, wraps every log line of the request in a
/// <c>CorrelationId</c> scope and travels in the <c>correlationId</c> member of problem responses,
/// so a client's error report can be matched with the server logs.
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    /// <summary>Request and response header that carries the id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Problem-details extension that carries the id.</summary>
    public const string ProblemExtension = "correlationId";

    /// <summary>Longest id accepted from a client.</summary>
    public const int MaxLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>Creates the middleware.</summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>The correlation id of the request, or <see langword="null"/> before the middleware ran.</summary>
    public static string? Get(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return httpContext.Features.Get<ICorrelationIdFeature>()?.CorrelationId;
    }

    /// <summary>Adds the request's id to a problem response (for <c>ProblemDetailsOptions.CustomizeProblemDetails</c>).</summary>
    public static void AddToProblem(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (Get(context.HttpContext) is { } correlationId)
        {
            context.ProblemDetails.Extensions[ProblemExtension] = correlationId;
        }
    }

    /// <summary>Whether a client-supplied id is safe to echo and log: 1 to 64 letters, digits, dots, hyphens or underscores.</summary>
    public static bool IsWellFormed(string? candidate) => candidate is not null && WellFormedId().IsMatch(candidate);

    /// <summary>Runs the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var supplied = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsWellFormed(supplied) ? supplied : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Features.Set<ICorrelationIdFeature>(new CorrelationIdFeature(correlationId));

        // Set at send time, so it survives the exception handler clearing the response.
        context.Response.OnStarting(static state =>
        {
            var (response, id) = ((HttpResponse, string))state;
            response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        }, (context.Response, correlationId));

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex WellFormedId();

    private sealed record CorrelationIdFeature(string CorrelationId) : ICorrelationIdFeature;
}

/// <summary>The correlation id of the current request, as a request feature.</summary>
public interface ICorrelationIdFeature
{
    /// <summary>The id.</summary>
    string CorrelationId { get; }
}

/// <summary>Pipeline registration for <see cref="CorrelationIdMiddleware"/>.</summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>Adds the correlation id middleware; place it first so every later component sees the id.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
