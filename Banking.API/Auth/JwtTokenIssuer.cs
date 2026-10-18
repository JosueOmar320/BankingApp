using System.Globalization;
using System.Security.Claims;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Banking.API.Auth;

/// <summary>
/// Issues HS256-signed JWTs. The subject (<c>sub</c>) is the customer id, which
/// <see cref="HttpContextCurrentCustomer"/> reads back on later requests; <c>name</c> carries the
/// display name for convenience and nothing else personal travels in the token.
/// </summary>
public sealed class JwtTokenIssuer : IAccessTokenIssuer
{
    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the issuer.</summary>
    public JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public AccessToken Issue(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, customer.CustomerId.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Name, customer.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_options.SigningKeyBytes), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAt);
    }
}
