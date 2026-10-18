using System.Net;
using System.Net.Http.Json;
using Banking.API.Localization;
using Banking.API.RateLimiting;
using Banking.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

/// <summary>
/// Login and registration are limited per client address, money operations per customer. A
/// rejected request gets a translated 429 problem with a Retry-After header. These fixtures host
/// their own API with small limits so the shared fixtures are not affected.
/// </summary>
public class RateLimitingTests
{
    private const int AuthenticationLimit = 3;
    private const int MoneyOperationsLimit = 2;

    private BankingApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void StartApi()
    {
        _factory = new BankingApiFactory(settings: new Dictionary<string, string?>
        {
            [$"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.Authentication)}:{nameof(RateLimitPolicyOptions.PermitLimit)}"] = AuthenticationLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [$"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.MoneyOperations)}:{nameof(RateLimitPolicyOptions.PermitLimit)}"] = MoneyOperationsLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void StopApi()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Login_BeyondTheLimit_Returns429ProblemWithRetryAfter()
    {
        var login = new LoginRequestDto { DocumentNumber = "nobody", Password = "wrong-password" };
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < AuthenticationLimit; attempt++)
        {
            using var allowed = await _client.PostAsJsonAsync("/api/Auth/login", login, ApiTestBase.Json);
            statuses.Add(allowed.StatusCode);
        }

        using var rejected = await _client.PostAsJsonAsync("/api/Auth/login", login, ApiTestBase.Json);

        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestBase.Json);
        Assert.Multiple(() =>
        {
            Assert.That(statuses, Has.All.EqualTo(HttpStatusCode.Unauthorized), "wrong credentials are rejected, but still counted");
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(rejected.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(rejected.Headers.RetryAfter?.Delta, Is.Not.Null.And.GreaterThan(TimeSpan.Zero));
            Assert.That(problem!.Title, Is.EqualTo("Too many requests"));
            Assert.That(problem.Extensions["code"]?.ToString(), Is.EqualTo(ApiErrorCodes.TooManyRequests));
            Assert.That(problem.Extensions, Contains.Key("correlationId"));
        });
    }

    [Test]
    public async Task Rejection_IsTranslated_WhenTheClientAsksForSpanish()
    {
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es");
        var login = new LoginRequestDto { DocumentNumber = "nobody", Password = "wrong-password" };
        for (var attempt = 0; attempt < AuthenticationLimit; attempt++)
        {
            using var _ = await _client.PostAsJsonAsync("/api/Auth/login", login, ApiTestBase.Json);
        }

        using var rejected = await _client.PostAsJsonAsync("/api/Auth/login", login, ApiTestBase.Json);

        var problem = await rejected.Content.ReadFromJsonAsync<ProblemDetails>(ApiTestBase.Json);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(problem!.Title, Is.EqualTo("Demasiadas solicitudes"));
            Assert.That(rejected.Content.Headers.ContentLanguage, Does.Contain("es-NI"));
        });
    }

    [Test]
    public async Task MoneyOperations_BeyondTheLimit_Return429_ForThatCustomerOnly()
    {
        // Registration + login use two of the three authentication permits per customer set-up, so the
        // second customer is created with its own factory-wide window still open (limit 3 per address).
        var (ownerToken, account) = await RegisterCustomerWithAccountAsync("DOC-OWNER");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ownerToken);

        var deposit = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 1m };
        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < MoneyOperationsLimit + 1; attempt++)
        {
            using var response = await _client.PostAsJsonAsync("/api/Transaction/Deposit", deposit, ApiTestBase.Json);
            statuses.Add(response.StatusCode);
        }

        // Reads are not limited, and the balance only reflects the accepted deposits.
        var balance = await _client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", ApiTestBase.Json);

        Assert.Multiple(() =>
        {
            Assert.That(statuses.Take(MoneyOperationsLimit), Has.All.EqualTo(HttpStatusCode.OK));
            Assert.That(statuses[^1], Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(balance!.Balance, Is.EqualTo(100m + MoneyOperationsLimit));
        });
    }

    private async Task<(string Token, BankAccountResponseDto Account)> RegisterCustomerWithAccountAsync(string documentNumber)
    {
        using var registered = await _client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Ana",
            LastName = "Pérez",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = documentNumber,
            Gender = Banking.Domain.Enums.Gender.F,
            MonthlyIncome = 1500m,
            Password = ApiTestBase.DefaultPassword,
        }, ApiTestBase.Json);
        registered.EnsureSuccessStatusCode();
        var customer = (await registered.Content.ReadFromJsonAsync<CustomerResponseDto>(ApiTestBase.Json))!;

        using var loggedIn = await _client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto { DocumentNumber = documentNumber, Password = ApiTestBase.DefaultPassword }, ApiTestBase.Json);
        loggedIn.EnsureSuccessStatusCode();
        var token = (await loggedIn.Content.ReadFromJsonAsync<LoginResponseDto>(ApiTestBase.Json))!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        using var opened = await _client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = 100m }, ApiTestBase.Json);
        opened.EnsureSuccessStatusCode();
        var account = (await opened.Content.ReadFromJsonAsync<BankAccountResponseDto>(ApiTestBase.Json))!;

        return (token, account);
    }
}
