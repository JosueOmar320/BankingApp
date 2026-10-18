using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Banking.Application.Dtos;
using Banking.Domain.Enums;

namespace Banking.Tests.API.Integration;

/// <summary>
/// One API host per fixture and one <see cref="Client"/> shared by its tests. The API requires a
/// bearer token, so <see cref="CreateCustomerAsync"/> registers a customer, logs in and leaves the
/// client acting as that customer; tests that need another caller register another customer, and
/// tests that need an anonymous caller call <see cref="UseToken"/> with <see langword="null"/>.
/// </summary>
public abstract class ApiTestBase
{
    /// <summary>Password of every customer registered through <see cref="CreateCustomerAsync"/>.</summary>
    protected const string DefaultPassword = "Secret123!";

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

    /// <summary>Registers a customer, logs in as them and makes <see cref="Client"/> send their token.</summary>
    protected async Task<CustomerResponseDto> CreateCustomerAsync(string? documentNumber = null, string password = DefaultPassword)
    {
        var response = await Client.PostAsJsonAsync("/api/Customer", new CreateCustomerDto
        {
            FirstName = "Ana",
            LastName = "Pérez",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = documentNumber ?? $"DOC-{Guid.NewGuid():N}",
            Gender = Gender.F,
            MonthlyIncome = 1500m,
            Password = password,
        }, Json);

        response.EnsureSuccessStatusCode();
        var customer = (await response.Content.ReadFromJsonAsync<CustomerResponseDto>(Json))!;

        var login = await LoginAsync(customer.DocumentNumber, password);
        UseToken(login.AccessToken);
        return customer;
    }

    /// <summary>Opens an account for a new customer; <see cref="Client"/> is left acting as its owner.</summary>
    protected async Task<BankAccountResponseDto> CreateAccountAsync(decimal initialDeposit = 100m)
    {
        var customer = await CreateCustomerAsync();
        var response = await Client.PostAsJsonAsync("/api/BankAccount", new CreateBankAccountDto { CustomerId = customer.CustomerId, InitialDeposit = initialDeposit }, Json);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BankAccountResponseDto>(Json))!;
    }

    /// <summary>Logs in and returns the token response; does not change <see cref="Client"/>.</summary>
    protected async Task<LoginResponseDto> LoginAsync(string documentNumber, string password = DefaultPassword)
    {
        var response = await Client.PostAsJsonAsync("/api/Auth/login", new LoginRequestDto { DocumentNumber = documentNumber, Password = password }, Json);

        Assert.That(response.IsSuccessStatusCode, Is.True, $"login failed: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<LoginResponseDto>(Json))!;
    }

    /// <summary>Makes <see cref="Client"/> send the token on every request, or nothing when <see langword="null"/>.</summary>
    protected void UseToken(string? accessToken)
        => Client.DefaultRequestHeaders.Authorization = accessToken is null ? null : new AuthenticationHeaderValue("Bearer", accessToken);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
