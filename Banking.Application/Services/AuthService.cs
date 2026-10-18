using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <inheritdoc cref="IAuthService"/>
internal sealed class AuthService : IAuthService
{
    private const string BearerTokenType = "Bearer";

    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenIssuer tokenIssuer,
        ILogger<AuthService> logger)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _customerRepository.GetCustomerByDocumentNumberAsync(request.DocumentNumber.Trim(), cancellationToken);
        if (customer is null)
        {
            // Hash anyway so an unknown document costs the same as a wrong password: response
            // times then do not reveal which document numbers are registered.
            _passwordHasher.Hash(request.Password);
            Log.LoginRejectedUnknownDocument(_logger);
            throw new InvalidCredentialsException();
        }

        if (!customer.VerifyPassword(request.Password, _passwordHasher))
        {
            Log.LoginRejected(_logger, customer.CustomerId);
            throw new InvalidCredentialsException();
        }

        var token = _tokenIssuer.Issue(customer);

        Log.LoginSucceeded(_logger, customer.CustomerId);
        return new LoginResponseDto(token.Value, BearerTokenType, token.ExpiresAtUtc, customer.CustomerId);
    }
}
