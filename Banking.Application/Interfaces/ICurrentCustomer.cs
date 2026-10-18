namespace Banking.Application.Interfaces;

/// <summary>
/// The customer behind the current request, as established by the host's authentication. The use
/// cases compare it with the owner of the data they touch; the host decides how it is obtained
/// (a JWT claim in the API, a fixed value in tests).
/// </summary>
public interface ICurrentCustomer
{
    /// <summary>Identifier of the authenticated customer, or <see langword="null"/> for an anonymous request.</summary>
    int? CustomerId { get; }
}
