using System.Net;
using System.Net.Http.Json;
using Banking.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Tests.API.Integration;

public class BankAccountEndpointsTests : ApiTestBase
{
    [Test]
    public async Task PostBankAccount_Returns201WithAccountAndLocation()
    {
        var customer = await CreateCustomerAsync();

        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = 150.75m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var account = await response.Content.ReadFromJsonAsync<BankAccountResponseDto>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(account!.AccountNumber, Has.Length.EqualTo(10));
            Assert.That(account.CustomerId, Is.EqualTo(customer.CustomerId));
            Assert.That(account.Balance, Is.EqualTo(150.75m));
            Assert.That(account.InterestRate, Is.EqualTo(0.10m));
            Assert.That(response.Headers.Location!.ToString(), Does.EndWith($"/api/BankAccount/{account.AccountNumber}"));
        });
    }

    [Test]
    public async Task PostBankAccount_UnknownCustomer_Returns404()
    {
        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = 999999, InitialDeposit = 100m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostBankAccount_DepositBelowMinimum_Returns400BusinessRuleProblem()
    {
        var customer = await CreateCustomerAsync();

        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = 9.99m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.That(problem!.Detail, Does.Contain("at least 10.00"));
    }

    [Test]
    public async Task PostBankAccount_InvalidPayload_Returns400ValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = 0, InitialDeposit = -1m }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors.Keys, Does.Contain("CustomerId").And.Contain("InitialDeposit"));
    }

    [Test]
    public async Task GetAccountAndBalance_ReturnTheAccount()
    {
        var account = await CreateAccountAsync(80m);

        var fetched = await Client.GetFromJsonAsync<BankAccountResponseDto>($"/api/BankAccount/{account.AccountNumber}", Json);
        var balance = await Client.GetFromJsonAsync<AccountBalanceDto>($"/api/BankAccount/{account.AccountNumber}/balance", Json);

        Assert.Multiple(() =>
        {
            Assert.That(fetched, Is.EqualTo(account));
            Assert.That(balance, Is.EqualTo(new AccountBalanceDto(account.AccountNumber, 80m)));
        });
    }

    [Test]
    public async Task GetBalance_UnknownAccount_Returns404()
    {
        var response = await Client.GetAsync("/api/BankAccount/0000000000/balance");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Health_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.EqualTo("Healthy"));
        });
    }
}
