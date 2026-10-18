using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Application.Mapping;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <inheritdoc cref="ICustomerService"/>
internal sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentCustomer _currentCustomer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        ICurrentCustomer currentCustomer,
        TimeProvider timeProvider,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _currentCustomer = currentCustomer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var entity = Customer.Create(
            customer.FirstName,
            customer.LastName,
            customer.BirthDate,
            customer.DocumentNumber,
            customer.Gender,
            customer.MonthlyIncome,
            customer.Password,
            _passwordHasher,
            _timeProvider.GetUtcNow().UtcDateTime);

        var saved = await _customerRepository.AddCustomerAsync(entity, cancellationToken);

        Log.CustomerRegistered(_logger, saved.CustomerId);
        return saved.ToDto();
    }

    public async Task<CustomerResponseDto> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetCustomerByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Customer", customerId);
        _currentCustomer.EnsureIs(customerId);

        return customer.ToDto();
    }
}
