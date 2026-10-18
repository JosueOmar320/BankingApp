using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Banking.Infrastructure.Security;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by ASP.NET Core Identity's hasher: PBKDF2 with HMAC-SHA512,
/// 128-bit salt, 100,000 iterations (format version 3). Each hash embeds its own salt and parameters,
/// so the algorithm can be strengthened later without invalidating stored hashes.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // Identity's hasher takes a user only to let derived hashers vary per user; this one does not.
    private static readonly Customer NoUser = null!;

    private readonly PasswordHasher<Customer> _hasher = new();

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _hasher.HashPassword(NoUser, password);
    }

    /// <inheritdoc />
    public bool Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            // SuccessRehashNeeded still means the password matched; the hash was produced by an older format.
            return _hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Not a hash this implementation produced (corrupted or legacy value): never a match.
            return false;
        }
    }
}
