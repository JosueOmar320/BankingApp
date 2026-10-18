using Banking.Application.Dtos;
using Banking.Application.Exceptions;

namespace Banking.Application.Interfaces;

/// <summary>Authentication use cases.</summary>
public interface IAuthService
{
    /// <summary>Checks a customer's credentials and issues an access token.</summary>
    /// <exception cref="InvalidCredentialsException">The document number or the password is wrong.</exception>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
