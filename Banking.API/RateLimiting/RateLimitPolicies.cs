using System.Globalization;
using System.Threading.RateLimiting;
using Banking.API.Localization;
using Banking.API.Middleware;
using Banking.API.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

namespace Banking.API.RateLimiting;

/// <summary>
/// The API's rate-limiting policies and their registration. A rejected request gets a translated
/// <c>429</c> problem response with a <c>Retry-After</c> header, like every other error.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Policy for login and registration: a fixed window per client IP address.</summary>
    public const string Authentication = "authentication";

    /// <summary>Policy for deposits, withdrawals and interest: a fixed window per authenticated customer.</summary>
    public const string MoneyOperations = "money-operations";

    /// <summary>Partition key when neither a customer nor an address is known (tests, proxies without forwarding).</summary>
    private const string UnknownPartition = "unknown";

    /// <summary>Binds <see cref="RateLimitingOptions"/> and registers the rate limiter with both policies.</summary>
    public static IServiceCollection AddBankingRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;

            limiter.AddPolicy(Authentication, httpContext =>
                FixedWindow(ClientAddress(httpContext), Options(httpContext).Authentication));

            limiter.AddPolicy(MoneyOperations, httpContext =>
                FixedWindow(CustomerOrAddress(httpContext), Options(httpContext).MoneyOperations));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(string partitionKey, RateLimitPolicyOptions policy)
        => RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = policy.Window,
            QueueLimit = 0,
            AutoReplenishment = true,
        });

    private static RateLimitingOptions Options(HttpContext httpContext)
        => httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static string ClientAddress(HttpContext httpContext)
        => httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownPartition;

    /// <summary>The customer id from the validated token; the address for anonymous callers, who are rejected by authorization anyway.</summary>
    private static string CustomerOrAddress(HttpContext httpContext)
    {
        var subject = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return string.IsNullOrEmpty(subject) ? ClientAddress(httpContext) : "customer:" + subject;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<ErrorMessages>>();

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.ContentLanguage = CultureInfo.CurrentUICulture.Name;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : (TimeSpan?)null;
        if (retryAfter is { } delay)
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] = Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = localizer[ProblemTitles.TooManyRequests],
            Detail = localizer[ApiErrorCodes.TooManyRequests],
            Instance = httpContext.Request.Path,
        };
        problem.Extensions[ValidationProblemResponse.CodeExtension] = ApiErrorCodes.TooManyRequests;

        await httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem });
    }
}
