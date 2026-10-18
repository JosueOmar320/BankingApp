using System.Net;
using System.Net.Http.Json;
using Banking.API.Idempotency;
using Banking.API.Localization;
using Banking.Application.Dtos;
using Banking.Domain.Enums;
using Banking.Infrastructure.Idempotency;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

/// <summary>
/// The optional <c>Idempotency-Key</c> header on money operations: a retry with the same key and
/// body gets the first response back and the ledger moves once; the same key with another body or
/// route is rejected; keys are private to each customer; a failed attempt releases the key.
/// </summary>
public class IdempotencyTests : ApiTestBase
{
    [Test]
    public async Task Deposit_RepeatedWithTheSameKeyAndBody_ReplaysTheFirstResponseAndMovesTheBalanceOnce()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 25.50m };

        using var first = await PostWithKeyAsync("/api/Transaction/Deposit", request, key);
        using var second = await PostWithKeyAsync("/api/Transaction/Deposit", request, key);

        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        var summary = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary", Json);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK), firstBody);
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK), secondBody);
            Assert.That(secondBody, Is.EqualTo(firstBody), "the replay is byte-for-byte the first response");
            Assert.That(second.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            Assert.That(first.Headers.Contains(IdempotencyFilter.ReplayedHeaderName), Is.False);
            Assert.That(second.Headers.GetValues(IdempotencyFilter.ReplayedHeaderName).Single(), Is.EqualTo("true"));
            Assert.That(balance!.Balance, Is.EqualTo(125.50m), "the deposit was applied once");
            Assert.That(summary!.Transactions, Has.Count.EqualTo(2), "opening deposit + one deposit");
        });
    }

    [Test]
    public async Task Deposit_WithoutAKey_IsAppliedEveryTime()
    {
        var account = await CreateAccountAsync(100m);
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m };

        await PostAsync<TransactionResponseDto>("/api/Transaction/Deposit", request);
        await PostAsync<TransactionResponseDto>("/api/Transaction/Deposit", request);

        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.That(balance!.Balance, Is.EqualTo(120m));
    }

    [Test]
    public async Task Deposit_SameKeyWithADifferentBody_Returns422AndAppliesNothing()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();

        using var first = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m }, key);
        using var second = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 11m }, key);

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            Assert.That(second.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(problem!.Title, Is.EqualTo("Idempotency key reused"));
            Assert.That(problem.Detail, Does.Contain(key));
            Assert.That(problem.Extensions["code"]!.ToString(), Is.EqualTo(ApiErrorCodes.IdempotencyKeyReused));
            Assert.That(balance!.Balance, Is.EqualTo(110m), "only the first deposit was applied");
        });
    }

    [Test]
    public async Task SameKeyOnAnotherOperationWithTheSameBody_Returns422()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m };

        using var deposit = await PostWithKeyAsync("/api/Transaction/Deposit", request, key);
        using var withdrawal = await PostWithKeyAsync("/api/Transaction/Withdrawal", request, key);

        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(deposit.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(withdrawal.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity), "the route is part of the request fingerprint");
            Assert.That(balance!.Balance, Is.EqualTo(110m));
        });
    }

    [Test]
    public async Task SameKeyWithADifferentBody_InSpanish_IsTranslated()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();
        using var first = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m }, key);

        using var second = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 11m }, key, acceptLanguage: "es");

        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
            Assert.That(problem!.Title, Is.EqualTo("Clave de idempotencia reutilizada"));
            Assert.That(problem.Detail, Does.StartWith($"La clave de idempotencia '{key}'"));
            Assert.That(problem.Extensions["code"]!.ToString(), Is.EqualTo(ApiErrorCodes.IdempotencyKeyReused));
        });
    }

    [Test]
    public async Task TheSameKey_UsedByTwoCustomers_IsTwoIndependentOperations()
    {
        var key = NewKey();
        var luisAccount = await CreateAccountAsync(100m);
        using var luisDeposit = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = luisAccount.AccountNumber, Amount = 10m }, key);

        var anaAccount = await CreateAccountAsync(100m); // the client now acts as another customer
        using var anaDeposit = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = anaAccount.AccountNumber, Amount = 20m }, key);

        var anaBalance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{anaAccount.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(luisDeposit.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(anaDeposit.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a different body under the same key is fine for a different customer");
            Assert.That(anaDeposit.Headers.Contains(IdempotencyFilter.ReplayedHeaderName), Is.False);
            Assert.That(anaBalance!.Balance, Is.EqualTo(120m));
        });
    }

    [Test]
    public async Task AFailedAttempt_ReleasesTheKey_SoTheSameRequestCanBeRetriedOnceTheCauseIsFixed()
    {
        var account = await CreateAccountAsync(20m);
        var key = NewKey();
        var withdrawal = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 50m };

        using var rejected = await PostWithKeyAsync("/api/Transaction/Withdrawal", withdrawal, key);
        await PostAsync<TransactionResponseDto>("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 100m });
        using var retried = await PostWithKeyAsync("/api/Transaction/Withdrawal", withdrawal, key);

        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "insufficient funds");
            Assert.That(retried.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the key was not consumed by the failure");
            Assert.That(retried.Headers.Contains(IdempotencyFilter.ReplayedHeaderName), Is.False);
            Assert.That(balance!.Balance, Is.EqualTo(70m));
        });
    }

    [Test]
    public async Task Transfer_RepeatedWithTheSameKey_CreditsTheDestinationOnce()
    {
        var destinationOwner = await CreateCustomerAsync();
        var destination = await OpenAccountAsync(destinationOwner.CustomerId, 50m);
        var source = await CreateAccountAsync(100m);
        var key = NewKey();
        var request = new TransferRequestDto { SourceAccountNumber = source.AccountNumber, DestinationAccountNumber = destination.AccountNumber, Amount = 30m };

        using var first = await PostWithKeyAsync("/api/Transaction/Transfer", request, key);
        using var second = await PostWithKeyAsync("/api/Transaction/Transfer", request, key);

        var firstTransfer = await first.Content.ReadFromJsonAsync<TransferResponseDto>(Json);
        var secondTransfer = await second.Content.ReadFromJsonAsync<TransferResponseDto>(Json);
        var sourceBalance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{source.AccountNumber}/balance", Json);

        UseToken((await LoginAsync(destinationOwner.DocumentNumber)).AccessToken);
        var destinationBalance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{destination.AccountNumber}/balance", Json);

        Assert.Multiple(() =>
        {
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(secondTransfer, Is.EqualTo(firstTransfer));
            Assert.That(sourceBalance!.Balance, Is.EqualTo(70m));
            Assert.That(destinationBalance!.Balance, Is.EqualTo(80m));
        });
    }

    [Test]
    public async Task ApplyInterest_RepeatedWithTheSameKey_CreditsInterestOnce()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();
        var request = new ApplyInterestRequestDto { AccountNumber = account.AccountNumber };

        using var first = await PostWithKeyAsync("/api/Transaction/Apply-Interest", request, key);
        using var second = await PostWithKeyAsync("/api/Transaction/Apply-Interest", request, key);

        var replayed = await second.Content.ReadFromJsonAsync<TransactionResponseDto>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(replayed!.TransactionType, Is.EqualTo(TransactionType.Interest));
            Assert.That(replayed.BalanceAfter, Is.EqualTo(110m));
            Assert.That(balance!.Balance, Is.EqualTo(110m), "10% once, not twice");
        });
    }

    [Test]
    public async Task AMalformedKey_Returns400ProblemWithoutApplyingTheOperation()
    {
        var account = await CreateAccountAsync(100m);
        var tooLong = new string('k', IdempotencyKey.MaxKeyLength + 1);

        using var response = await PostWithKeyAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m }, tooLong);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Extensions["code"]!.ToString(), Is.EqualTo(ApiErrorCodes.IdempotencyKeyInvalid));
            Assert.That(problem.Detail, Does.Contain(IdempotencyKey.MaxKeyLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(balance!.Balance, Is.EqualTo(100m));
        });
    }

    [Test]
    public async Task TwoSimultaneousRequestsWithTheSameKey_ApplyTheOperationOnce()
    {
        var account = await CreateAccountAsync(100m);
        var key = NewKey();
        var request = new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m };

        var responses = await Task.WhenAll(
            PostWithKeyAsync("/api/Transaction/Deposit", request, key),
            PostWithKeyAsync("/api/Transaction/Deposit", request, key));

        var statuses = responses.Select(response => response.StatusCode).ToList();
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(statuses, Has.Exactly(1).EqualTo(HttpStatusCode.OK).Or.Exactly(2).EqualTo(HttpStatusCode.OK));
            Assert.That(statuses, Has.All.EqualTo(HttpStatusCode.OK).Or.Some.EqualTo(HttpStatusCode.Conflict), "the loser of the race is replayed once the first finished, or told it is still running");
            Assert.That(balance!.Balance, Is.EqualTo(110m), "never applied twice");
        });

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Test]
    public async Task Summary_IsNotIdempotencyKeyAware_AndIgnoresTheHeader()
    {
        var account = await CreateAccountAsync(100m);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Transaction/{account.AccountNumber}/summary");
        request.Headers.Add(IdempotencyFilter.HeaderName, new string('k', IdempotencyKey.MaxKeyLength + 1));

        using var response = await Client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "reads do not validate the header");
    }

    private static string NewKey() => Guid.NewGuid().ToString();

    private async Task<T> PostAsync<T>(string route, object body)
    {
        var response = await Client.PostAsJsonAsync(route, body, Json);
        Assert.That(response.IsSuccessStatusCode, Is.True, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<HttpResponseMessage> PostWithKeyAsync<T>(string route, T body, string key, string? acceptLanguage = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.Add(IdempotencyFilter.HeaderName, key);
        if (acceptLanguage is not null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        return await Client.SendAsync(request);
    }
}
