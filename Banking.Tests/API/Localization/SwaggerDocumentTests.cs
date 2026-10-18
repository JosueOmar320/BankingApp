using System.Net;
using System.Text.Json;
using Banking.API;
using Banking.API.Auth;
using Banking.API.Idempotency;
using Banking.API.Localization;

namespace Banking.Tests.API.Localization;

/// <summary>
/// The OpenAPI document advertises the Accept-Language header on every operation and the bearer
/// scheme on every protected one, so the API can be driven from Swagger UI.
/// </summary>
public class SwaggerDocumentTests
{
    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new BankingApiFactory(environment: "Development");
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task SwaggerJson_ReportsTheAssemblyVersion()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var info = document.RootElement.GetProperty("info");

        Assert.Multiple(() =>
        {
            Assert.That(ApiVersion.Current, Does.Match(@"^\d+\.\d+\.\d+$"), "the version comes from <Version> in Directory.Build.props");
            Assert.That(info.GetProperty("title").GetString(), Is.EqualTo("Banking API"));
            Assert.That(info.GetProperty("version").GetString(), Is.EqualTo(ApiVersion.Current));
        });
    }

    [Test]
    public async Task SwaggerJson_DocumentsAcceptLanguageOnEveryOperation()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operations = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (Route: path.Name + " " + operation.Name, Operation: operation.Value)))
            .ToList();

        Assert.That(operations, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            foreach (var (route, operation) in operations)
            {
                var header = operation.GetProperty("parameters").EnumerateArray()
                    .SingleOrDefault(parameter => parameter.GetProperty("name").GetString() == "Accept-Language");

                Assert.That(header.ValueKind, Is.EqualTo(JsonValueKind.Object), $"{route} has no Accept-Language parameter");
                Assert.That(header.GetProperty("in").GetString(), Is.EqualTo("header"), route);
                Assert.That(header.TryGetProperty("required", out var required) && required.GetBoolean(), Is.False, $"{route}: the header is optional");

                var schema = header.GetProperty("schema");
                Assert.That(schema.GetProperty("default").GetString(), Is.EqualTo(SupportedCultures.Default), route);
                Assert.That(schema.GetProperty("enum").EnumerateArray().Select(value => value.GetString()), Is.EquivalentTo(SupportedCultures.AcceptLanguageExamples), route);
            }
        });
    }

    [Test]
    public async Task SwaggerJson_DeclaresTheBearerScheme_AndRequiresItOnProtectedOperationsOnly()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty(BearerSecurityOperationFilter.SchemeName);
        var operations = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (Route: path.Name + " " + operation.Name, Operation: operation.Value)))
            .ToDictionary(pair => pair.Route, pair => pair.Operation);

        var anonymous = new[] { "/api/Auth/login post", "/api/Customer post" };

        Assert.Multiple(() =>
        {
            Assert.That(scheme.GetProperty("type").GetString(), Is.EqualTo("http"));
            Assert.That(scheme.GetProperty("scheme").GetString(), Is.EqualTo("bearer"));
            Assert.That(scheme.GetProperty("bearerFormat").GetString(), Is.EqualTo("JWT"));

            foreach (var (route, operation) in operations)
            {
                var requiresToken = operation.TryGetProperty("security", out var security)
                    && security.EnumerateArray().Any(requirement => requirement.TryGetProperty(BearerSecurityOperationFilter.SchemeName, out _));
                var responses = operation.GetProperty("responses");

                if (anonymous.Contains(route))
                {
                    Assert.That(requiresToken, Is.False, $"{route} must stay anonymous");
                }
                else
                {
                    Assert.That(requiresToken, Is.True, $"{route} must require the bearer token");
                    Assert.That(responses.TryGetProperty("401", out _), Is.True, $"{route} documents 401");
                    Assert.That(responses.TryGetProperty("403", out _), Is.True, $"{route} documents 403");
                }
            }
        });
    }

    [Test]
    public async Task SwaggerJson_DocumentsTheIdempotencyKeyHeader_OnMoneyOperationsOnly()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var operations = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => (Route: path.Name + " " + operation.Name, Operation: operation.Value)))
            .ToDictionary(pair => pair.Route, pair => pair.Operation);

        var moneyOperations = new[] { "/api/Transaction/Deposit post", "/api/Transaction/Withdrawal post", "/api/Transaction/Apply-Interest post", "/api/Transaction/Transfer post" };

        Assert.That(operations.Keys, Is.SupersetOf(moneyOperations));
        Assert.Multiple(() =>
        {
            foreach (var (route, operation) in operations)
            {
                var header = operation.GetProperty("parameters").EnumerateArray()
                    .SingleOrDefault(parameter => parameter.GetProperty("name").GetString() == IdempotencyFilter.HeaderName);
                var documents422 = operation.GetProperty("responses").TryGetProperty("422", out _);

                if (moneyOperations.Contains(route))
                {
                    Assert.That(header.ValueKind, Is.EqualTo(JsonValueKind.Object), $"{route} accepts {IdempotencyFilter.HeaderName}");
                    Assert.That(header.GetProperty("in").GetString(), Is.EqualTo("header"), route);
                    Assert.That(header.TryGetProperty("required", out var required) && required.GetBoolean(), Is.False, $"{route}: the header is optional");
                    Assert.That(header.GetProperty("schema").GetProperty("maxLength").GetInt32(), Is.EqualTo(Banking.Infrastructure.Idempotency.IdempotencyKey.MaxKeyLength), route);
                    Assert.That(documents422, Is.True, $"{route} documents 422");
                }
                else
                {
                    Assert.That(header.ValueKind, Is.EqualTo(JsonValueKind.Undefined), $"{route} is not idempotent-key aware");
                    Assert.That(documents422, Is.False, $"{route} does not document 422");
                }
            }
        });
    }
}
