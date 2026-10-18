using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Banking.Application.Dtos;
using Banking.Domain.Enums;

namespace Banking.Tests.API.Integration;

public abstract class ApiTestBase
{
    protected static readonly JsonSerializerOptions Json = CreateJsonOptions();

    private BankingApiFactory _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public void StartApi()
    {
        _factory = new BankingApiFactory();
        Client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void StopApi()
    {
        Client.Dispose();
        _factory.Dispose();
    }

    protected async Task<CustomerResponseDto> CreateCustomerAsync(string? documentNumber = null)
    {
        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Ana",
            LastName = "Pérez",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = documentNumber ?? $"DOC-{Guid.NewGuid():N}",
            Gender = Gender.F,
            MonthlyIncome = 1500m,
        }, Json);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponseDto>(Json))!;
    }

    protected async Task<BankAccountResponseDto> CreateAccountAsync(decimal initialDeposit = 100m)
    {
        var customer = await CreateCustomerAsync();
        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = initialDeposit }, Json);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BankAccountResponseDto>(Json))!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
