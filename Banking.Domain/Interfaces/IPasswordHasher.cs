namespace Banking.Domain.Interfaces;

/// <summary>
/// Turns a password into a one-way hash and checks a password against a stored hash. The domain
/// only ever handles hashes; the algorithm (and its upgrades) belongs to the infrastructure.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a password with a fresh salt. The same password never produces the same hash twice.</summary>
    string Hash(string password);

    /// <summary>Checks a password against a hash produced by <see cref="Hash"/>.</summary>
    bool Verify(string passwordHash, string password);
}
