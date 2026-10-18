using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Banking.API.Localization;

/// <summary>Documents the optional <c>Accept-Language</c> header on every operation in Swagger.</summary>
public sealed class AcceptLanguageOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Language of the error messages (titles, details and validation errors). "
                + $"Supported: {string.Join(", ", SupportedCultures.AcceptLanguageExamples)}; "
                + $"any other Spanish or English variant maps to {SupportedCultures.Spanish} or {SupportedCultures.Default}. "
                + $"Default: {SupportedCultures.Default}.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString(SupportedCultures.Default),
                Enum = SupportedCultures.AcceptLanguageExamples.Select(IOpenApiAny (value) => new OpenApiString(value)).ToList(),
            },
        });
    }
}
