namespace Banking.Application.Dtos;

/// <summary>An access token for the authenticated customer.</summary>
/// <param name="AccessToken">JWT to send as <c>Authorization: Bearer &lt;accessToken&gt;</c>.</param>
/// <param name="TokenType">Always <c>Bearer</c>.</param>
/// <param name="ExpiresAt">UTC instant after which the token is rejected.</param>
/// <param name="CustomerId">Identifier of the authenticated customer.</param>
public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    int CustomerId);
