using Banking.API.Localization;
using Banking.API.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace Banking.API.Middleware;

/// <summary>
/// Builds the automatic <c>400</c> response of <c>[ApiController]</c> for an invalid model state:
/// the same shape as <see cref="ValidationProblemDetails"/> (field messages already come translated
/// from the DTO resources) with a translated title and the <c>code</c> extension, written through
/// <see cref="IProblemDetailsService"/> like every other problem response so it is
/// <c>application/problem+json</c> regardless of the controller's <c>[Produces]</c> filter.
/// </summary>
public static class ValidationProblemResponse
{
    /// <summary>Name of the problem-details extension that carries the stable error code.</summary>
    public const string CodeExtension = "code";

    /// <summary>Creates the response for <see cref="ApiBehaviorOptions.InvalidModelStateResponseFactory"/>.</summary>
    public static IActionResult Create(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.HttpContext.RequestServices;
        var localizer = services.GetRequiredService<IStringLocalizer<ErrorMessages>>();
        var factory = services.GetRequiredService<ProblemDetailsFactory>();

        // A plain ProblemDetails with the field errors as the standard "errors" member: the problem-details
        // writer serialises the base type, so the Errors property of ValidationProblemDetails would be lost.
        var problem = factory.CreateProblemDetails(
            context.HttpContext,
            StatusCodes.Status400BadRequest,
            title: localizer[ProblemTitles.ValidationFailed]);
        problem.Extensions["errors"] = new ValidationProblemDetails(context.ModelState).Errors;
        problem.Extensions[CodeExtension] = ApiErrorCodes.ValidationFailed;

        return new ProblemDetailsResult(problem);
    }

    /// <summary>Writes a problem with <see cref="IProblemDetailsService"/>, falling back to MVC's object result.</summary>
    private sealed class ProblemDetailsResult : IActionResult
    {
        private readonly ProblemDetails _problem;

        public ProblemDetailsResult(ProblemDetails problem)
        {
            _problem = problem;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var httpContext = context.HttpContext;
            httpContext.Response.StatusCode = _problem.Status ?? StatusCodes.Status400BadRequest;

            var written = await httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>()
                .TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = _problem });

            if (!written)
            {
                // The client does not accept JSON: let MVC's content negotiation answer.
                await new ObjectResult(_problem) { StatusCode = httpContext.Response.StatusCode }.ExecuteResultAsync(context);
            }
        }
    }
}
