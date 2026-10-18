using System.Globalization;
using Banking.API.Localization;
using Banking.API.Resources;
using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Banking.API.Middleware;

/// <summary>
/// Translates exceptions into RFC 7807 problem responses with a stable mapping:
/// <list type="bullet">
/// <item><see cref="InvalidCredentialsException"/> → 401</item>
/// <item><see cref="ForbiddenException"/> → 403</item>
/// <item><see cref="NotFoundException"/> → 404</item>
/// <item><see cref="DuplicateDocumentNumberException"/>, <see cref="ConcurrencyConflictException"/> → 409</item>
/// <item><see cref="DomainException"/> (business rule violated) → 400</item>
/// <item>anything else → 500 without leaking internals</item>
/// </list>
/// The title and detail are returned in the request's culture (see <see cref="AcceptLanguageCultureProvider"/>),
/// looked up by the exception's <see cref="IHasErrorCode.Code"/>, and the code itself travels in the
/// <c>code</c> extension so clients never have to parse the text.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IStringLocalizer<ErrorMessages> localizer,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _localizer = localizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (status, titleKey) = exception switch
        {
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, ProblemTitles.InvalidCredentials),
            ForbiddenException => (StatusCodes.Status403Forbidden, ProblemTitles.Forbidden),
            NotFoundException => (StatusCodes.Status404NotFound, ProblemTitles.NotFound),
            DuplicateDocumentNumberException => (StatusCodes.Status409Conflict, ProblemTitles.DuplicateDocumentNumber),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, ProblemTitles.ConcurrencyConflict),
            InsufficientFundsException => (StatusCodes.Status400BadRequest, ProblemTitles.InsufficientFunds),
            DomainException => (StatusCodes.Status400BadRequest, ProblemTitles.BusinessRuleViolated),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                => (StatusCodes.Status499ClientClosedRequest, ProblemTitles.RequestCancelled),
            _ => (StatusCodes.Status500InternalServerError, ProblemTitles.UnexpectedError),
        };

        var code = exception switch
        {
            IHasErrorCode coded => coded.Code,
            _ when status == StatusCodes.Status499ClientClosedRequest => ApiErrorCodes.RequestCancelled,
            _ => ApiErrorCodes.UnexpectedError,
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(_logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandled(_logger, httpContext.Request.Method, httpContext.Request.Path, status, code, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        // The exception-handler middleware cleared the response, including the Content-Language header
        // that the localization middleware had set; put it back so the client knows the text's language.
        httpContext.Response.Headers.ContentLanguage = CultureInfo.CurrentUICulture.Name;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = _localizer[titleKey],
            Detail = status == StatusCodes.Status500InternalServerError ? null : Detail(exception, code),
            Instance = httpContext.Request.Path,
        };
        problem.Extensions[ValidationProblemResponse.CodeExtension] = code;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    /// <summary>
    /// The translated text for the error code, formatted with the exception's arguments. Falls back to
    /// the exception's (English) message when no resource exists for the code.
    /// </summary>
    private string Detail(Exception exception, string code)
    {
        var arguments = exception is IHasErrorCode coded ? coded.MessageArguments : [];
        var localized = arguments.Count == 0 ? _localizer[code] : _localizer[code, arguments.Select(argument => argument ?? string.Empty).ToArray()];

        return localized.ResourceNotFound ? exception.Message : localized.Value;
    }

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Request {Method} {Path} failed with {StatusCode} ({Code}): {Message}")]
    private static partial void LogHandled(ILogger logger, string method, PathString path, int statusCode, string code, string message);
}
