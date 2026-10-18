using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Banking.API.Localization;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

/// <summary>
/// Login issues a JWT; every other endpoint needs it (401 otherwise) and only lets a customer
/// reach their own data (403 otherwise). Errors are problem responses like everywhere else.
/// </summary>
public class AuthEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Login_WithValidCredentials_ReturnsBearerToken()
    {
        var customer = await CreateCustomerAsync();
        var before = DateTime.UtcNow;

        var login = await LoginAsync(customer.DocumentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(login.TokenType, Is.EqualTo("Bearer"));
            Assert.That(login.CustomerId, Is.EqualTo(customer.CustomerId));
            Assert.That(login.AccessToken.Split('.'), Has.Length.EqualTo(3), "a compact JWS: header.payload.signature");
            Assert.That(login.ExpiresAt, Is.GreaterThan(before.AddMinutes(30)).And.LessThanOrEqualTo(before.AddHours(24)));
            Assert.That(login.ExpiresAt.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public async Task Login_WithWrongPassword_Returns401Problem_WithoutSayingWhich()
    {
        var customer = await CreateCustomerAsync();

        var problem = await PostProblemAsync("/api/Auth/login", new LoginRequestDto { DocumentNumber = customer.DocumentNumber, Password = "not the password" });

        Assert.Multiple(() =>
        {
            Assert.That(problem.Status, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(problem.Details.Title, Is.EqualTo("Invalid credentials"));
            Assert.That(problem.Details.Detail, Is.EqualTo("The document number or the password is incorrect."));
            Assert.That(problem.Code, Is.EqualTo(ApplicationErrorCodes.InvalidCredentials));
        });
    }

    [Test]
    public async Task Login_WithUnknownDocument_ReturnsTheSame401()
    {
        var problem = await PostProblemAsync("/api/Auth/login", new LoginRequestDto { DocumentNumber = "NOBODY-HOME", Password = DefaultPassword });

        Assert.Multiple(() =>
        {
            Assert.That(problem.Status, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(problem.Code, Is.EqualTo(ApplicationErrorCodes.InvalidCredentials));
            Assert.That(problem.Details.Detail, Does.Not.Contain("NOBODY-HOME"));
        });
    }

    [Test]
    public async Task Login_InSpanish_IsTranslated()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/login")
        {
            Content = JsonContent.Create(new LoginRequestDto { DocumentNumber = "NOBODY-HOME", Password = DefaultPassword }, options: Json),
        };
        request.Headers.Add("Accept-Language", "es");

        using var response = await Client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(problem!.Title, Is.EqualTo("Credenciales inválidas"));
            Assert.That(problem.Detail, Is.EqualTo("El número de documento o la contraseña son incorrectos."));
        });
    }

    [Test]
    public async Task Login_WithEmptyPassword_Returns400ValidationProblem()
    {
        using var content = JsonContent.Create(new { documentNumber = "DOC-1", password = string.Empty });

        var response = await Client.PostAsync("/api/Auth/login", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors, Contains.Key("Password"));
    }

    [Test]
    public async Task ProtectedEndpoints_WithoutAToken_Return401Problem_WithChallenge()
    {
        UseToken(null);

        using var response = await Client.GetAsync("/api/BankAccount/1234567890/balance");
        var payload = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(payload, Json)!;

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Is.EqualTo("Bearer"));
            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"), payload);
            Assert.That(response.Content.Headers.ContentLanguage.SingleOrDefault(), Is.EqualTo("en-US"));
            Assert.That(problem.Title, Is.EqualTo("Authentication required"));
            Assert.That(problem.Detail, Does.Contain("POST /api/Auth/login"));
            Assert.That(Code(problem), Is.EqualTo(ApiErrorCodes.AuthenticationRequired));
        });
    }

    [Test]
    public async Task ProtectedEndpoints_WithoutAToken_Return401InSpanish()
    {
        UseToken(null);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Transaction/Deposit")
        {
            Content = JsonContent.Create(new TransactionRequestDto { AccountNumber = "1234567890", Amount = 1m }, options: Json),
        };
        request.Headers.Add("Accept-Language", "es-NI");

        using var response = await Client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Content.Headers.ContentLanguage.SingleOrDefault(), Is.EqualTo("es-NI"));
            Assert.That(problem!.Title, Is.EqualTo("Autenticación requerida"));
        });
    }

    [Test]
    public async Task ProtectedEndpoints_WithATamperedToken_Return401()
    {
        var customer = await CreateCustomerAsync();
        var login = await LoginAsync(customer.DocumentNumber);
        var parts = login.AccessToken.Split('.');
        UseToken($"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}");

        using var response = await Client.GetAsync($"/api/Customer/{customer.CustomerId}");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Headers.WwwAuthenticate.ToString(), Does.StartWith("Bearer").And.Contain("invalid_token"));
        });
    }

    [Test]
    public async Task ProtectedEndpoints_WithATokenSignedByAnotherKey_Return401()
    {
        var customer = await CreateCustomerAsync();
        var login = await LoginAsync(customer.DocumentNumber);
        var parts = login.AccessToken.Split('.');
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}""")).TrimEnd('=');
        UseToken($"{header}.{parts[1]}.");

        using var response = await Client.GetAsync($"/api/Customer/{customer.CustomerId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "unsigned tokens are rejected");
    }

    [Test]
    public async Task AccountOperations_WithAnotherCustomersToken_Return403_AndChangeNothing()
    {
        var account = await CreateAccountAsync(100m);
        var owner = await Client.GetFromJsonAsync<BankAccountResponseDto>($"/api/BankAccount/{account.AccountNumber}", Json);
        var ownerLogin = await LoginAsync((await Client.GetFromJsonAsync<CustomerResponseDto>($"/api/Customer/{owner!.CustomerId}", Json))!.DocumentNumber);
        await CreateCustomerAsync();

        var number = account.AccountNumber;
        var results = new Dictionary<string, ProblemResult>
        {
            ["GET account"] = await GetProblemAsync($"/api/BankAccount/{number}"),
            ["GET balance"] = await GetProblemAsync($"/api/BankAccount/{number}/balance"),
            ["GET summary"] = await GetProblemAsync($"/api/Transaction/{number}/summary"),
            ["POST Deposit"] = await PostProblemAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = number, Amount = 10m }),
            ["POST Withdrawal"] = await PostProblemAsync("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = number, Amount = 10m }),
            ["POST Apply-Interest"] = await PostProblemAsync("/api/Transaction/Apply-Interest", new ApplyInterestRequestDto { AccountNumber = number }),
        };

        UseToken(ownerLogin.AccessToken);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{number}/balance", Json);
        var summary = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{number}/summary", Json);

        Assert.Multiple(() =>
        {
            foreach (var (operation, result) in results)
            {
                Assert.That(result.Status, Is.EqualTo(HttpStatusCode.Forbidden), operation);
                Assert.That(result.Details.Title, Is.EqualTo("Access denied"), operation);
                Assert.That(result.Details.Detail, Is.EqualTo($"Account '{number}' belongs to another customer."), operation);
                Assert.That(result.Code, Is.EqualTo(ApplicationErrorCodes.AccountAccessDenied), operation);
            }

            Assert.That(balance!.Balance, Is.EqualTo(100m), "the intruder changed nothing");
            Assert.That(summary!.Transactions, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task AccountOperations_WithTheOwnersToken_Return200()
    {
        var account = await CreateAccountAsync(100m);

        var fetched = await Client.GetAsync($"/api/BankAccount/{account.AccountNumber}");
        var balance = await Client.GetAsync($"/api/BankAccount/{account.AccountNumber}/balance");
        var deposit = await Client.PostAsJsonAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m }, Json);
        var withdrawal = await Client.PostAsJsonAsync("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 5m }, Json);
        var interest = await Client.PostAsJsonAsync("/api/Transaction/Apply-Interest", new ApplyInterestRequestDto { AccountNumber = account.AccountNumber }, Json);
        var summary = await Client.GetAsync($"/api/Transaction/{account.AccountNumber}/summary");

        Assert.Multiple(() =>
        {
            Assert.That(fetched.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(balance.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deposit.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(withdrawal.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(interest.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(summary.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task GetCustomer_WithOwnToken_Returns200()
    {
        var customer = await CreateCustomerAsync();

        var fetched = await Client.GetFromJsonAsync<CustomerResponseDto>($"/api/Customer/{customer.CustomerId}", Json);

        Assert.That(fetched, Is.EqualTo(customer));
    }

    private static string? Code(ProblemDetails problem)
        => problem.Extensions.TryGetValue("code", out var value) && value is JsonElement element ? element.GetString() : null;

    private Task<ProblemResult> GetProblemAsync(string url) => SendProblemAsync(HttpMethod.Get, url, null);

    private Task<ProblemResult> PostProblemAsync(string url, object body) => SendProblemAsync(HttpMethod.Post, url, body);

    private async Task<ProblemResult> SendProblemAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        using var response = await Client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"), payload);

        var problem = JsonSerializer.Deserialize<ProblemDetails>(payload, Json)!;
        return new ProblemResult(response.StatusCode, problem, Code(problem));
    }

    private sealed record ProblemResult(HttpStatusCode Status, ProblemDetails Details, string? Code);
}
