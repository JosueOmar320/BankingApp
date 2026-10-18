using System.Net;
using System.Net.Http.Json;
using Banking.Application.Dtos;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

public class TransactionEndpointsTests : ApiTestBase
{
    [Test]
    public async Task FullLedgerFlow_KeepsBalanceAndSummaryConsistent()
    {
        var account = await CreateAccountAsync(100m);

        var deposit = await PostAsync<TransactionResponseDto>("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 50.50m });
        var withdrawal = await PostAsync<TransactionResponseDto>("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 30m });
        var interest = await PostAsync<TransactionResponseDto>("/api/Transaction/Apply-Interest", new ApplyInterestRequestDto { AccountNumber = account.AccountNumber });

        Assert.Multiple(() =>
        {
            Assert.That(deposit.TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(deposit.BalanceAfter, Is.EqualTo(150.50m));
            Assert.That(withdrawal.TransactionType, Is.EqualTo(TransactionType.Withdrawal));
            Assert.That(withdrawal.BalanceAfter, Is.EqualTo(120.50m));
            Assert.That(interest.TransactionType, Is.EqualTo(TransactionType.Interest));
            Assert.That(interest.Amount, Is.EqualTo(12.05m));
            Assert.That(interest.BalanceAfter, Is.EqualTo(132.55m));
        });

        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        var summary = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary", Json);

        Assert.Multiple(() =>
        {
            Assert.That(balance!.Balance, Is.EqualTo(132.55m));
            Assert.That(summary!.StartingBalance, Is.Zero);
            Assert.That(summary.TotalDeposits, Is.EqualTo(150.50m));
            Assert.That(summary.TotalWithdrawals, Is.EqualTo(30m));
            Assert.That(summary.TotalInterest, Is.EqualTo(12.05m));
            Assert.That(summary.FinalBalance, Is.EqualTo(132.55m));
            Assert.That(summary.Transactions, Has.Count.EqualTo(4));
            Assert.That(summary.Transactions.Select(t => t.TransactionType), Is.EqualTo(new[] { TransactionType.Deposit, TransactionType.Deposit, TransactionType.Withdrawal, TransactionType.Interest }));
            Assert.That(summary.Transactions[^1].BalanceAfter, Is.EqualTo(132.55m));
        });
    }

    [Test]
    public async Task Withdrawal_AboveBalance_Returns400AndLeavesBalanceUntouched()
    {
        var account = await CreateAccountAsync(20m);

        var response = await Client.PostAsJsonAsync("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 20.01m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        Assert.Multiple(() =>
        {
            Assert.That(problem!.Title, Is.EqualTo("Insufficient funds"));
            Assert.That(balance!.Balance, Is.EqualTo(20m));
        });
    }

    [TestCase(0)]
    [TestCase(-5)]
    public async Task Deposit_NonPositiveAmount_Returns400ValidationProblem(decimal amount)
    {
        var account = await CreateAccountAsync();

        var response = await Client.PostAsJsonAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = amount }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors, Contains.Key("Amount"));
    }

    [Test]
    public async Task Deposit_MoreThanTwoDecimals_Returns400BusinessRuleProblem()
    {
        var account = await CreateAccountAsync();

        var response = await Client.PostAsJsonAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 1.005m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.That(problem!.Detail, Does.Contain("decimal places"));
    }

    [Test]
    public async Task Operations_OnUnknownAccount_Return404()
    {
        await CreateCustomerAsync();

        var deposit = await Client.PostAsJsonAsync("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = "0000000000", Amount = 1m }, Json);
        var withdrawal = await Client.PostAsJsonAsync("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = "0000000000", Amount = 1m }, Json);
        var interest = await Client.PostAsJsonAsync("/api/Transaction/Apply-Interest", new ApplyInterestRequestDto { AccountNumber = "0000000000" }, Json);
        var summary = await Client.GetAsync("/api/Transaction/0000000000/summary");

        Assert.Multiple(() =>
        {
            Assert.That(deposit.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(withdrawal.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(interest.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(summary.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task ApplyInterest_OnEmptyAccount_Returns400()
    {
        var account = await CreateAccountAsync(10m);
        await PostAsync<TransactionResponseDto>("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 10m });

        var response = await Client.PostAsJsonAsync("/api/Transaction/Apply-Interest", new ApplyInterestRequestDto { AccountNumber = account.AccountNumber }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ConcurrentWithdrawals_NeverOverdrawTheAccount()
    {
        var account = await CreateAccountAsync(100m);

        // Ten parallel withdrawals of 30 against a balance of 100: at most three can succeed.
        var attempts = Enumerable.Range(0, 10)
            .Select(_ => Client.PostAsJsonAsync("/api/Transaction/Withdrawal", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = 30m }, Json));
        var responses = await Task.WhenAll(attempts);

        var statuses = responses.Select(r => r.StatusCode).ToList();
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);
        var summary = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary", Json);

        var succeeded = statuses.Count(s => s == HttpStatusCode.OK);
        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.InRange(1, 3));
            Assert.That(statuses, Has.All.AnyOf(HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.BadRequest));
            Assert.That(balance!.Balance, Is.EqualTo(100m - (30m * succeeded)));
            Assert.That(balance.Balance, Is.GreaterThanOrEqualTo(0m));
            Assert.That(summary!.Transactions, Has.Count.EqualTo(1 + succeeded), "every successful withdrawal, and nothing else, is in the ledger");
        });
    }

    [Test]
    public async Task Summary_ReturnsTheRequestedPageAndTotalsOverTheWholeLedger()
    {
        var account = await CreateAccountAsync(100m);
        foreach (var amount in new[] { 1m, 2m, 3m, 4m })
        {
            await PostAsync<TransactionResponseDto>("/api/Transaction/Deposit", new TransactionRequestDto { AccountNumber = account.AccountNumber, Amount = amount });
        }

        var first = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary?page=1&pageSize=2", Json);
        var last = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary?page=3&pageSize=2", Json);
        var whole = await Client.GetFromJsonAsync<AccountTransactionSummaryDto>($"/api/Transaction/{account.AccountNumber}/summary", Json);

        Assert.Multiple(() =>
        {
            Assert.That(first!.Transactions.Select(t => t.Amount), Is.EqualTo(new List<decimal> { 100m, 1m }));
            Assert.That(first.Page, Is.EqualTo(new PageInfoDto(Number: 1, Size: 2, TotalItems: 5, TotalPages: 3)));
            Assert.That(first.TotalDeposits, Is.EqualTo(110m), "totals cover the whole ledger, not just the page");
            Assert.That(first.FinalBalance, Is.EqualTo(110m));
            Assert.That(last!.Transactions.Select(t => t.Amount), Is.EqualTo(new List<decimal> { 4m }));
            Assert.That(last.Page, Is.EqualTo(new PageInfoDto(3, 2, 5, 3)));
            Assert.That(whole!.Transactions, Has.Count.EqualTo(5));
            Assert.That(whole.Page, Is.EqualTo(new PageInfoDto(1, LedgerPageRequestDto.DefaultPageSize, 5, 1)));
        });
    }

    [TestCase("page=0", "Page")]
    [TestCase("pageSize=0", "PageSize")]
    [TestCase("pageSize=201", "PageSize")]
    public async Task Summary_WithPageOutOfRange_Returns400ValidationProblem(string query, string field)
    {
        var account = await CreateAccountAsync();

        var response = await Client.GetAsync($"/api/Transaction/{account.AccountNumber}/summary?{query}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors, Contains.Key(field));
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await Client.PostAsJsonAsync(url, body, Json);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }
}
