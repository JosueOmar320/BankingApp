using Banking.Domain.Interfaces;

namespace Banking.Tests.Support;

/// <summary>
/// A transparent, instant <see cref="IPasswordHasher"/> for domain and service tests: the "hash"
/// is the password behind a prefix, so tests can assert on it without PBKDF2 cost. The real
/// hasher has its own tests.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public const string Prefix = "hashed:";

    public int HashCalls { get; private set; }

    public string Hash(string password)
    {
        HashCalls++;
        return Prefix + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == Prefix + password;
}
