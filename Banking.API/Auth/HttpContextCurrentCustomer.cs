using System.Globalization;
using Banking.Application.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Banking.API.Auth;

/// <summary>
/// Reads the authenticated customer from the <c>sub</c> claim of the validated bearer token.
/// Anonymous requests (and tokens without a numeric subject) have no customer.
/// </summary>
public sealed class HttpContextCurrentCustomer : ICurrentCustomer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates the accessor.</summary>
    public HttpContextCurrentCustomer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public int? CustomerId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var customerId) ? customerId : null;
        }
    }
}
