using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;

namespace Banking.Application.Interfaces;

/// <summary>Bank account use cases.</summary>
public interface IBankAccountService
{
    /// <summary>Opens an account for an existing customer with an opening deposit.</summary>
    /// <exception cref="NotFoundException">The customer does not exist.</exception>
    /// <exception cref="DomainException">The opening deposit violates a business rule.</exception>
    Task<BankAccountResponseDto> CreateBankAccountAsync(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default);

    /// <summary>Gets an account by its number.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    Task<BankAccountResponseDto> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>Gets the balance of an account.</summary>
    /// <exception cref="NotFoundException">The account does not exist.</exception>
    Task<AccountBalanceDto> GetBalanceByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
}
