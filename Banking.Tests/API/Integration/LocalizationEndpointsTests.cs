using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.API.Localization;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

/// <summary>
/// The same error, requested in English and in Spanish, comes back translated (title and detail),
/// with the culture in <c>Content-Language</c> and the stable code in the <c>code</c> extension.
/// </summary>
public class LocalizationEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Withdrawal_InsufficientFunds_IsTranslatedByAcceptLanguage()
    {
        var account = await CreateAccountAsync(20m);
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 20.01m };

        var english = await PostProblemAsync("/api/Transaction/Withdrawal", request, "en-US");
        var spanish = await PostProblemAsync("/api/Transaction/Withdrawal", request, "es-NI");

        Assert.Multiple(() =>
        {
            Assert.That(english.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(english.Language, Is.EqualTo("en-US"));
            Assert.That(english.Problem.Title, Is.EqualTo("Insufficient funds"));
            Assert.That(english.Problem.Detail, Is.EqualTo("Insufficient funds: the available balance is 20.00 and 20.01 was requested."));
            Assert.That(english.Code, Is.EqualTo(DomainErrorCodes.InsufficientFunds));

            Assert.That(spanish.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(spanish.Language, Is.EqualTo("es-NI"));
            Assert.That(spanish.Problem.Title, Is.EqualTo("Fondos insuficientes"));
            Assert.That(spanish.Problem.Detail, Is.EqualTo("Fondos insuficientes: el saldo disponible es 20.00 y se solicitó 20.01."));
            Assert.That(spanish.Code, Is.EqualTo(DomainErrorCodes.InsufficientFunds), "the code never changes with the language");
        });
    }

    [Test]
    public async Task GetCustomer_Unknown_IsTranslatedByAcceptLanguage()
    {
        var english = await GetProblemAsync("/api/Customer/999999", "en");
        var spanish = await GetProblemAsync("/api/Customer/999999", "es");

        Assert.Multiple(() =>
        {
            Assert.That(english.Status, Is.EqualTo((int)HttpStatusCode.NotFound));
            Assert.That(english.Problem.Title, Is.EqualTo("Resource not found"));
            Assert.That(english.Problem.Detail, Is.EqualTo("Customer '999999' was not found."));
            Assert.That(english.Code, Is.EqualTo(ApplicationErrorCodes.CustomerNotFound));

            Assert.That(spanish.Status, Is.EqualTo((int)HttpStatusCode.NotFound));
            Assert.That(spanish.Language, Is.EqualTo("es-NI"), "plain 'es' maps to the supported Spanish culture");
            Assert.That(spanish.Problem.Title, Is.EqualTo("Recurso no encontrado"));
            Assert.That(spanish.Problem.Detail, Is.EqualTo("No se encontró el cliente '999999'."));
            Assert.That(spanish.Code, Is.EqualTo(ApplicationErrorCodes.CustomerNotFound));
        });
    }

    [Test]
    public async Task PostCustomer_DuplicateDocument_IsTranslatedInSpanish()
    {
        var document = $"DUP-ES-{Guid.NewGuid():N}";
        await CreateCustomerAsync(document);

        var spanish = await PostProblemAsync("/api/Customer", NewCustomer(document), "es-NI");

        Assert.Multiple(() =>
        {
            Assert.That(spanish.Status, Is.EqualTo((int)HttpStatusCode.Conflict));
            Assert.That(spanish.Problem.Title, Is.EqualTo("Número de documento duplicado"));
            Assert.That(spanish.Problem.Detail, Is.EqualTo($"Ya existe un cliente con el número de documento '{document}'."));
            Assert.That(spanish.Code, Is.EqualTo(DomainErrorCodes.DuplicateDocumentNumber));
        });
    }

    [Test]
    public async Task PostBankAccount_DepositBelowMinimum_IsTranslatedInSpanish()
    {
        var customer = await CreateCustomerAsync();

        var spanish = await PostProblemAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = 9.99m }, "es");

        Assert.Multiple(() =>
        {
            Assert.That(spanish.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(spanish.Problem.Title, Is.EqualTo("Regla de negocio incumplida"));
            Assert.That(spanish.Problem.Detail, Is.EqualTo("El depósito inicial debe ser de al menos 10.00."));
            Assert.That(spanish.Code, Is.EqualTo(DomainErrorCodes.DepositBelowMinimum));
        });
    }

    [Test]
    public async Task ValidationErrors_AreTranslatedByAcceptLanguage()
    {
        var invalid = new CreateBankAccountDto { CustomerId = 0, InitialDeposit = -1m };

        var english = await PostValidationProblemAsync("/api/BankAccount", invalid, "en-US");
        var spanish = await PostValidationProblemAsync("/api/BankAccount", invalid, "es-NI");

        Assert.Multiple(() =>
        {
            Assert.That(english.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(english.Problem.Title, Is.EqualTo("One or more validation errors occurred."));
            Assert.That(english.Problem.Errors["CustomerId"], Has.One.EqualTo("The customer identifier must be a positive integer."));
            Assert.That(english.Problem.Errors["InitialDeposit"], Has.One.EqualTo("The initial deposit must be greater than zero."));
            Assert.That(english.Code, Is.EqualTo(ApiErrorCodes.ValidationFailed));

            Assert.That(spanish.Status, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(spanish.Problem.Title, Is.EqualTo("Se produjeron uno o más errores de validación."));
            Assert.That(spanish.Problem.Errors["CustomerId"], Has.One.EqualTo("El identificador del cliente debe ser un entero positivo."));
            Assert.That(spanish.Problem.Errors["InitialDeposit"], Has.One.EqualTo("El depósito inicial debe ser mayor que cero."));
            Assert.That(spanish.Code, Is.EqualTo(ApiErrorCodes.ValidationFailed));
        });
    }

    [Test]
    public async Task ValidationErrors_WithPlaceholders_UseTheAttributeArguments()
    {
        var tooLong = new TransactionRequestDto { AccountNumber = new string('1', 33), Amount = 1m };

        var spanish = await PostValidationProblemAsync("/api/Transaction/Deposit", tooLong, "es");

        Assert.That(spanish.Problem.Errors["AccountNumber"], Has.One.EqualTo("El número de cuenta debe tener entre 1 y 32 caracteres."));
    }

    [TestCase(null, "en-US", TestName = "NoHeader_DefaultsToEnglish")]
    [TestCase("fr-FR", "en-US", TestName = "UnsupportedLanguage_DefaultsToEnglish")]
    [TestCase("en-GB", "en-US", TestName = "OtherEnglishRegion_MapsToEnUS")]
    [TestCase("es-MX", "es-NI", TestName = "OtherSpanishRegion_MapsToEsNI")]
    [TestCase("es-NI", "es-NI", TestName = "ExactCulture_IsUsed")]
    [TestCase("fr, es;q=0.8, en;q=0.5", "es-NI", TestName = "QualityOrder_PicksFirstSupported")]
    [TestCase("en;q=0.5, es;q=0.9", "es-NI", TestName = "QualityValue_OutranksHeaderOrder")]
    [TestCase("*", "en-US", TestName = "Wildcard_DefaultsToEnglish")]
    public async Task AcceptLanguage_SelectsTheResponseCulture(string? acceptLanguage, string expectedCulture)
    {
        var result = await GetProblemAsync("/api/BankAccount/0000000000/balance", acceptLanguage);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo((int)HttpStatusCode.NotFound));
            Assert.That(result.Language, Is.EqualTo(expectedCulture));
            Assert.That(result.Problem.Detail, Is.EqualTo(expectedCulture == "es-NI"
                ? "No se encontró la cuenta '0000000000'."
                : "Account '0000000000' was not found."));
        });
    }

    [Test]
    public async Task SuccessfulResponses_AreNotAffectedByTheLanguage()
    {
        var account = await CreateAccountAsync(100m);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/BankAccount/{account.AccountNumber}/balance");
        request.Headers.Add("Accept-Language", "es-NI");
        using var response = await Client.SendAsync(request);
        var balance = await response.Content.ReadFromJsonAsync<AccountBalanceDto>(Json);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(balance, Is.EqualTo(new AccountBalanceDto(account.AccountNumber, 100m)), "numbers stay culture-invariant JSON");
        });
    }

    private static CreateCustomerDto NewCustomer(string documentNumber) => new()
    {
        FirstName = "Ana",
        BirthDate = new DateOnly(1990, 1, 1),
        DocumentNumber = documentNumber,
        Gender = Gender.F,
    };

    private Task<ProblemResult<ProblemDetails>> GetProblemAsync(string url, string? acceptLanguage)
        => SendAsync<ProblemDetails>(HttpMethod.Get, url, body: null, acceptLanguage);

    private Task<ProblemResult<ProblemDetails>> PostProblemAsync(string url, object body, string? acceptLanguage)
        => SendAsync<ProblemDetails>(HttpMethod.Post, url, body, acceptLanguage);

    private Task<ProblemResult<ValidationProblemDetails>> PostValidationProblemAsync(string url, object body, string? acceptLanguage)
        => SendAsync<ValidationProblemDetails>(HttpMethod.Post, url, body, acceptLanguage);

    private async Task<ProblemResult<TProblem>> SendAsync<TProblem>(HttpMethod method, string url, object? body, string? acceptLanguage)
        where TProblem : ProblemDetails
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        if (acceptLanguage is not null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        using var response = await Client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"), payload);
        var problem = JsonSerializer.Deserialize<TProblem>(payload, Json)!;
        var code = problem.Extensions.TryGetValue("code", out var value) && value is JsonElement element ? element.GetString() : null;

        return new ProblemResult<TProblem>((int)response.StatusCode, response.Content.Headers.ContentLanguage.SingleOrDefault(), problem, code);
    }

    private sealed record ProblemResult<TProblem>(int Status, string? Language, TProblem Problem, string? Code)
        where TProblem : ProblemDetails;
}
