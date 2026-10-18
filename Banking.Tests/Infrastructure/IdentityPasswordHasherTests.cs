using Banking.Infrastructure.Security;

namespace Banking.Tests.Infrastructure;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Test]
    public void Hash_NeverContainsThePassword_AndIsSaltedPerCall()
    {
        var first = _hasher.Hash("Correct horse battery");
        var second = _hasher.Hash("Correct horse battery");

        Assert.Multiple(() =>
        {
            Assert.That(first, Does.Not.Contain("Correct horse"));
            Assert.That(first, Is.Not.EqualTo(second), "a fresh salt per hash");
            Assert.That(first, Has.Length.LessThanOrEqualTo(Banking.Domain.Entities.Customer.MaxPasswordHashLength));
        });
    }

    [Test]
    public void Verify_AcceptsTheRightPasswordOnly()
    {
        var hash = _hasher.Hash("Correct horse battery");

        Assert.Multiple(() =>
        {
            Assert.That(_hasher.Verify(hash, "Correct horse battery"), Is.True);
            Assert.That(_hasher.Verify(hash, "correct horse battery"), Is.False);
            Assert.That(_hasher.Verify(hash, string.Empty), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("not-a-hash")]
    [TestCase("AQAAAA")]
    public void Verify_RejectsValuesThatAreNotHashes(string storedValue)
        => Assert.That(_hasher.Verify(storedValue, "anything"), Is.False);

    [Test]
    public void Hash_RequiresAPassword()
        => Assert.Throws<ArgumentException>(() => _hasher.Hash(string.Empty));
}
