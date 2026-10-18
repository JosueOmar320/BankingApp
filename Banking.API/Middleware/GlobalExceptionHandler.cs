using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Middleware;

/// <summary>
/// Translates exceptions into RFC 7807 problem responses with a stable mapping:
/// <list type="bullet">
/// <item><see cref="NotFoundException"/> → 404</item>
/// <item><see cref="DuplicateDocumentNumberException"/>, <see cref="ConcurrencyConflictException"/> → 409</item>
/// <item><see cref="DomainException"/> (business rule violated) → 400</item>
/// <item>anything else → 500 without leaking internals</item>
/// </list>
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            DuplicateDocumentNumberException => (StatusCodes.Status409Conflict, "Duplicate document number"),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Concurrent modification"),
            InsufficientFundsException => (StatusCodes.Status400BadRequest, "Insufficient funds"),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                => (StatusCodes.Status499ClientClosedRequest, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(_logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandled(_logger, httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            Instance = httpContext.Request.Path,
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Request {Method} {Path} failed with {StatusCode}: {Message}")]
    private static partial void LogHandled(ILogger logger, string method, PathString path, int statusCode, string message);
}
