using System.Text.Json.Nodes;
using Microsoft.OpenApi;
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
                Type = JsonSchemaType.String,
                Default = JsonValue.Create(SupportedCultures.Default),
                Enum = SupportedCultures.AcceptLanguageExamples.Select(JsonNode (value) => JsonValue.Create(value)).ToList(),
            },
        });
    }
}
