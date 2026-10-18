using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;

namespace Banking.Application.Services;

/// <summary>
/// The authorization rule of the whole API: a customer may only touch their own data. Each use
/// case calls these after loading the entity, so a missing one is still a 404 and someone else's
/// is a 403.
/// </summary>
internal static class OwnershipGuard
{
    /// <summary>Throws unless <paramref name="caller"/> owns the account.</summary>
    /// <exception cref="ForbiddenException">The account belongs to another customer.</exception>
    public static void EnsureOwnedBy(this BankAccount account, ICurrentCustomer caller)
    {
        if (caller.CustomerId != account.CustomerId)
        {
            throw new ForbiddenException("Account", account.AccountNumber);
        }
    }

    /// <summary>Throws unless <paramref name="caller"/> is the customer with <paramref name="customerId"/>.</summary>
    /// <exception cref="ForbiddenException">The customer is someone else.</exception>
    public static void EnsureIs(this ICurrentCustomer caller, int customerId)
    {
        if (caller.CustomerId != customerId)
        {
            throw new ForbiddenException("Customer", customerId);
        }
    }
}
