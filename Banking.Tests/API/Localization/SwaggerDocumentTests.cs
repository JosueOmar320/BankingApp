using System.Net;
using System.Text.Json;
using Banking.API.Localization;

namespace Banking.Tests.API.Localization;

/// <summary>The OpenAPI document advertises the Accept-Language header on every operation.</summary>
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
}
