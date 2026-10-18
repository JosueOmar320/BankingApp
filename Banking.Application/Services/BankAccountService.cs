using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <inheritdoc cref="IBankAccountService"/>
internal sealed class BankAccountService : IBankAccountService
{
    private const int MaxAccountNumberAttempts = 10;

    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BankAccountService> _logger;

    public BankAccountService(
        IBankAccountRepository bankAccountRepository,
        ICustomerRepository customerRepository,
        ICurrentCustomer currentCustomer,
        TimeProvider timeProvider,
        ILogger<BankAccountService> logger)
    {
        _bankAccountRepository = bankAccountRepository;
        _customerRepository = customerRepository;
        _currentCustomer = currentCustomer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bankAccountDto);

        // A customer opens accounts for themself only.
        _currentCustomer.EnsureIs(bankAccountDto.CustomerId);

        if (await _customerRepository.GetCustomerByIdAsync(bankAccountDto.CustomerId, cancellationToken) is null)
        {
            throw new NotFoundException("Customer", bankAccountDto.CustomerId);
        }

        var accountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
        var account = BankAccount.Open(accountNumber, bankAccountDto.CustomerId, bankAccountDto.InitialDeposit, _timeProvider.GetUtcNow().UtcDateTime);

        var saved = await _bankAccountRepository.AddBankAccountAsync(account, cancellationToken);

        Log.AccountOpened(_logger, saved.BankAccountId, saved.CustomerId);
        return saved.ToDto();
    }

    public async Task<BankAccountResponseDto> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await LoadOwnAccountAsync(accountNumber, cancellationToken);

        return account.ToDto();
    }

    public async Task<AccountBalanceDto> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await LoadOwnAccountAsync(accountNumber, cancellationToken);

        return new AccountBalanceDto(account.AccountNumber, account.Balance);
    }

    /// <summary>Loads an account that must exist (404) and belong to the caller (403).</summary>
    private async Task<BankAccount> LoadOwnAccountAsync(string accountNumber, CancellationToken cancellationToken)
    {
        var account = await _bankAccountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken)
            ?? throw new NotFoundException("Account", accountNumber);
        account.EnsureOwnedBy(_currentCustomer);

        return account;
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
    {
        // 10-digit numbers: [1_000_000_000, 9_999_999_999]
        var min = (long)Math.Pow(10, AccountRules.AccountNumberLength - 1);
        var max = (long)Math.Pow(10, AccountRules.AccountNumberLength);

        for (var attempt = 0; attempt < MaxAccountNumberAttempts; attempt++)
        {
            var candidate = Random.Shared.NextInt64(min, max).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!await _bankAccountRepository.ExistsByNumberAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not generate a unique account number after {MaxAccountNumberAttempts} attempts.");
    }
}
