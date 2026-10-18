using Banking.Domain.Entities;

namespace Banking.Application.Interfaces;

/// <summary>Creates the bearer token a customer presents on later requests. The format (JWT) is the host's choice.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>Issues a token that identifies <paramref name="customer"/> until it expires.</summary>
    AccessToken Issue(Customer customer);
}

/// <summary>A freshly issued bearer token.</summary>
/// <param name="Value">The serialized token, to be sent as <c>Authorization: Bearer &lt;value&gt;</c>.</param>
/// <param name="ExpiresAtUtc">Instant after which the token is rejected.</param>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
