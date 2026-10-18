using System.Net;
using System.Net.Http.Json;
using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

/// <summary>
/// POST /api/Transaction/Transfer moves money from the caller's account to any other account,
/// recording both sides atomically. Only the source has to be the caller's.
/// </summary>
public class TransferEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Transfer_ToAnotherCustomersAccount_UpdatesBothLedgers()
    {
        var destinationOwner = await CreateCustomerAsync();
        var destination = await OpenAccountAsync(destinationOwner.CustomerId, 50m);
        var source = await CreateAccountAsync(100m); // the client now acts as the source's owner

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = destination.AccountNumber,
            Amount = 30.25m,
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        var transfer = (await response.Content.ReadFromJsonAsync<TransferResponseDto>(Json))!;
        var sourceSummary = (await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{source.AccountNumber}/summary", Json))!;

        Assert.Multiple(() =>
        {
            Assert.That(transfer.SourceAccountNumber, Is.EqualTo(source.AccountNumber));
            Assert.That(transfer.DestinationAccountNumber, Is.EqualTo(destination.AccountNumber));
            Assert.That(transfer.Amount, Is.EqualTo(30.25m));
            Assert.That(transfer.SourceBalanceAfter, Is.EqualTo(69.75m));
            Assert.That(sourceSummary.FinalBalance, Is.EqualTo(69.75m));
            Assert.That(sourceSummary.TotalTransfersOut, Is.EqualTo(30.25m));
            Assert.That(sourceSummary.TotalTransfersIn, Is.Zero);
            Assert.That(sourceSummary.StartingBalance, Is.Zero);
            Assert.That(sourceSummary.Transactions[^1].TransactionType, Is.EqualTo(TransactionType.TransferOut));
        });

        // The destination's owner sees the credit on their side.
        UseToken((await LoginAsync(destinationOwner.DocumentNumber)).AccessToken);
        var destinationSummary = (await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{destination.AccountNumber}/summary", Json))!;

        Assert.Multiple(() =>
        {
            Assert.That(destinationSummary.FinalBalance, Is.EqualTo(80.25m));
            Assert.That(destinationSummary.TotalTransfersIn, Is.EqualTo(30.25m));
            Assert.That(destinationSummary.StartingBalance, Is.Zero);
            Assert.That(destinationSummary.Transactions[^1].TransactionType, Is.EqualTo(TransactionType.TransferIn));
            Assert.That(destinationSummary.Transactions[^1].TransactionDate, Is.EqualTo(transfer.TransactionDate), "both ledger entries share the instant");
        });
    }

    [Test]
    public async Task Transfer_ToTheSameAccount_Returns400WithSameAccountTransferCode()
    {
        var account = await CreateAccountAsync(100m);

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = account.AccountNumber,
            DestinationAccountNumber = account.AccountNumber,
            Amount = 10m,
        }, Json);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Extensions["code"]?.ToString(), Is.EqualTo(DomainErrorCodes.SameAccountTransfer));
            Assert.That(balance!.Balance, Is.EqualTo(100m));
        });
    }

    [Test]
    public async Task Transfer_AboveTheSourceBalance_Returns400AndChangesNothing()
    {
        var destination = await CreateAccountAsync(50m);
        var source = await CreateAccountAsync(20m);

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = destination.AccountNumber,
            Amount = 20.01m,
        }, Json);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{source.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Extensions["code"]?.ToString(), Is.EqualTo(DomainErrorCodes.InsufficientFunds));
            Assert.That(balance!.Balance, Is.EqualTo(20m));
        });
    }

    [Test]
    public async Task Transfer_ToAnUnknownAccount_Returns404()
    {
        var source = await CreateAccountAsync(100m);

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = "0000000000",
            Amount = 10m,
        }, Json);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(problem!.Extensions["code"]?.ToString(), Is.EqualTo(ApplicationErrorCodes.AccountNotFound));
        });
    }

    [Test]
    public async Task Transfer_FromAnotherCustomersAccount_Returns403AndChangesNothing()
    {
        var victim = await CreateAccountAsync(100m);
        var attacker = await CreateAccountAsync(10m); // the client now acts as the attacker

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = victim.AccountNumber,
            DestinationAccountNumber = attacker.AccountNumber,
            Amount = 50m,
        }, Json);

        var attackerBalance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{attacker.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(attackerBalance!.Balance, Is.EqualTo(10m));
        });
    }

    [Test]
    public async Task Transfer_WithInvalidFields_Returns400ValidationProblem()
    {
        await CreateCustomerAsync();

        var response = await Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto { SourceAccountNumber = "", DestinationAccountNumber = "", Amount = 0 }, Json);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(problem!.Errors, Contains.Key("SourceAccountNumber").And.ContainKey("DestinationAccountNumber").And.ContainKey("Amount"));
        });
    }

    [Test]
    public async Task ConcurrentTransfers_NeverOverdrawTheSource()
    {
        var destination = await CreateAccountAsync(10m);
        var source = await CreateAccountAsync(100m);

        // Ten parallel transfers of 30 against a balance of 100: at most three can succeed.
        var attempts = Enumerable.Range(0, 10).Select(_ => Client.PostAsJsonAsync("/api/Transaction/Transfer", new TransferRequestDto
        {
            SourceAccountNumber = source.AccountNumber,
            DestinationAccountNumber = destination.AccountNumber,
            Amount = 30m,
        }, Json));
        var responses = await Task.WhenAll(attempts);

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var sourceSummary = (await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{source.AccountNumber}/summary", Json))!;
        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.InRange(1, 3));
            Assert.That(responses.Select(r => r.StatusCode), Has.All.AnyOf(HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.BadRequest));
            Assert.That(sourceSummary.FinalBalance, Is.EqualTo(100m - (30m * succeeded)));
            Assert.That(sourceSummary.TotalTransfersOut, Is.EqualTo(30m * succeeded));
            Assert.That(sourceSummary.Page.TotalItems, Is.EqualTo(1 + succeeded), "every successful transfer, and nothing else, is in the source ledger");
        });
    }
}
