using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Banking.API.Auth;

/// <summary>
/// Marks every operation that is not <c>[AllowAnonymous]</c> as requiring the bearer token
/// (the padlock in Swagger UI) and documents its <c>401</c> and <c>403</c> problem responses.
/// </summary>
public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    /// <summary>Name of the security scheme registered with <c>AddSecurityDefinition</c>.</summary>
    public const string SchemeName = "Bearer";

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = [],
        });

        var problem = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        operation.Responses ??= [];
        operation.Responses.TryAdd("401", ProblemResponse("The request carries no valid bearer token.", problem));
        operation.Responses.TryAdd("403", ProblemResponse("The resource belongs to another customer.", problem));
    }

    private static OpenApiResponse ProblemResponse(string description, IOpenApiSchema schema) => new()
    {
        Description = description,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new OpenApiMediaType { Schema = schema },
        },
    };
}
