using System.Text.Json.Nodes;
using Banking.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Banking.API.Idempotency;

/// <summary>Documents the optional <c>Idempotency-Key</c> header and its error responses on every <see cref="IdempotentAttribute"/> operation.</summary>
public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IdempotentAttribute>().Any())
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = IdempotencyFilter.HeaderName,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Client-generated key (for example a UUID) that makes the request safe to retry. Repeating the same "
                + "key with the same body returns the first response again, with `Idempotent-Replayed: true`, without applying "
                + "the operation twice. The same key with a different body or route is rejected with 422; a key whose first "
                + "request is still running answers 409. Keys are kept per customer for 24 hours by default.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                MinLength = 1,
                MaxLength = IdempotencyKey.MaxKeyLength,
                Example = JsonValue.Create("8f0c6a1e-5b2d-4c3e-9f7a-1d2e3f4a5b6c"),
            },
        });

        var problem = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        operation.Responses ??= [];
        operation.Responses.TryAdd("422", new OpenApiResponse
        {
            Description = "The Idempotency-Key was already used for a different request.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = problem },
            },
        });
    }
}
