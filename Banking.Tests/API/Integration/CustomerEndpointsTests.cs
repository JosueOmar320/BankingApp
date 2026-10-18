using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

public class CustomerEndpointsTests : ApiTestBase
{
    [Test]
    public async Task PostCustomer_WithoutAToken_Returns201WithLocationAndBody()
    {
        UseToken(null);

        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Luis",
            LastName = "Hernández",
            BirthDate = new DateOnly(1985, 11, 3),
            DocumentNumber = "POST-201",
            Gender = Gender.M,
            MonthlyIncome = 2500m,
            Password = DefaultPassword,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<CustomerResponseDto>(raw, Json);
        Assert.That(body, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(body!.CustomerId, Is.GreaterThan(0));
            Assert.That(body.FirstName, Is.EqualTo("Luis"));
            Assert.That(body.Gender, Is.EqualTo(Gender.M));
            Assert.That(response.Headers.Location!.ToString(), Does.EndWith($"/api/Customer/{body.CustomerId}"));
            Assert.That(raw, Does.Not.Contain("assword").IgnoreCase, "neither the password nor its hash is ever returned");
        });
    }

    [Test]
    public async Task PostCustomer_ShortPassword_Returns400ValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Ana",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = $"SHORT-{Guid.NewGuid():N}",
            Gender = Gender.F,
            Password = "short",
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors["Password"], Has.One.EqualTo("The password must be between 8 and 128 characters long."));
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
    public async Task GetCustomer_AnotherCustomer_Returns403Problem()
    {
        var other = await CreateCustomerAsync();
        await CreateCustomerAsync();

        var response = await Client.GetAsync($"/api/Customer/{other.CustomerId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(problem!.Detail, Does.Contain(other.CustomerId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(problem.Extensions["code"]!.ToString(), Is.EqualTo(ApplicationErrorCodes.CustomerAccessDenied));
        });
    }

    [Test]
    public async Task GetCustomer_UnknownId_Returns404Problem()
    {
        await CreateCustomerAsync();

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
            Password = DefaultPassword,
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
            Password = DefaultPassword,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.That(problem!.Detail, Does.Contain("DUP-1"));
    }
}
