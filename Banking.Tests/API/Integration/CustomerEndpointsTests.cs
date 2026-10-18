using System.Net;
using System.Net.Http.Json;
using Banking.Application.Dtos;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

public class CustomerEndpointsTests : ApiTestBase
{
    [Test]
    public async Task PostCustomer_Returns201WithLocationAndBody()
    {
        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Luis",
            LastName = "Hernández",
            BirthDate = new DateOnly(1985, 11, 3),
            DocumentNumber = "POST-201",
            Gender = Gender.M,
            MonthlyIncome = 2500m,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<CustomerResponseDto>(Json);
        Assert.That(body, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(body!.CustomerId, Is.GreaterThan(0));
            Assert.That(body.FirstName, Is.EqualTo("Luis"));
            Assert.That(body.Gender, Is.EqualTo(Gender.M));
            Assert.That(response.Headers.Location!.ToString(), Does.EndWith($"/api/Customer/{body.CustomerId}"));
        });
    }

    [Test]
    public async Task GetCustomer_ReturnsCreatedCustomer()
    {
        var created = await CreateCustomerAsync();

        var fetched = await Client.GetFromJsonAsync<CustomerResponseDto>($"/api/Customer/{created.CustomerId}", Json);

        Assert.Multiple(() =>
        {
            Assert.That(fetched, Is.EqualTo(created));
            Assert.That(fetched!.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc), "timestamps are returned as UTC (trailing Z) after a database round-trip");
        });
    }

    [Test]
    public async Task GetCustomer_UnknownId_Returns404Problem()
    {
        var response = await Client.GetAsync("/api/Customer/999999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(problem!.Status, Is.EqualTo(404));
            Assert.That(problem.Detail, Does.Contain("999999"));
        });
    }

    [Test]
    public async Task PostCustomer_MissingRequiredFields_Returns400ValidationProblem()
    {
        using var content = JsonContent.Create(new { lastName = "Solo" });

        var response = await Client.PostAsync("/api/Customer", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors, Is.Not.Empty);
    }

    [Test]
    public async Task PostCustomer_InvalidGender_Returns400()
    {
        using var content = JsonContent.Create(new { firstName = "Ana", birthDate = "1990-01-01", documentNumber = "G-1", gender = "X" });

        var response = await Client.PostAsync("/api/Customer", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostCustomer_FutureBirthDate_Returns400BusinessRuleProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Ana",
            BirthDate = new DateOnly(2999, 1, 1),
            DocumentNumber = "FUTURE-1",
            Gender = Gender.F,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(problem!.Title, Is.EqualTo("Business rule violated"));
            Assert.That(problem.Detail, Does.Contain("future"));
        });
    }

    [Test]
    public async Task PostCustomer_DuplicateDocumentNumber_Returns409()
    {
        await CreateCustomerAsync("DUP-1");

        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Otro",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = "DUP-1",
            Gender = Gender.O,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.That(problem!.Detail, Does.Contain("DUP-1"));
    }
}
