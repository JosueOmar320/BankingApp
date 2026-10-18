using System.Globalization;
using Banking.API.Localization;
using Banking.API.Middleware;
using Banking.API.Resources;
using Banking.Application.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Net.Http.Headers;

namespace Banking.API.Auth;

/// <summary>
/// Makes the bearer handler answer like the rest of the API: a missing, expired or tampered token
/// gets a translated <c>401</c> problem response (plus the <c>WWW-Authenticate</c> challenge) instead
/// of an empty body, and a policy failure a <c>403</c> one.
/// </summary>
public static class AuthenticationProblemResponses
{
    /// <summary>Bearer events that write problem responses.</summary>
    public static JwtBearerEvents Events { get; } = new()
    {
        OnChallenge = async context =>
        {
            // Take over the default behaviour (status code + header, no body).
            context.HandleResponse();

            var challenge = context.AuthenticateFailure is null
                ? JwtBearerDefaults.AuthenticationScheme
                : $"{JwtBearerDefaults.AuthenticationScheme} error=\"invalid_token\"";
            context.Response.Headers[HeaderNames.WWWAuthenticate] = challenge;

            await WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, ProblemTitles.AuthenticationRequired, ApiErrorCodes.AuthenticationRequired);
        },
        OnForbidden = context => WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, ProblemTitles.Forbidden, ForbiddenException.DefaultCode),
    };

    private static async Task WriteAsync(HttpContext httpContext, int status, string titleKey, string code)
    {
        var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<ErrorMessages>>();

        httpContext.Response.StatusCode = status;
        httpContext.Response.Headers.ContentLanguage = CultureInfo.CurrentUICulture.Name;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = localizer[titleKey],
            Detail = localizer[code],
            Instance = httpContext.Request.Path,
        };
        problem.Extensions[ValidationProblemResponse.CodeExtension] = code;

        await httpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem });
    }
}
