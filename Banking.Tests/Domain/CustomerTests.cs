using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Tests.Support;

namespace Banking.Tests.Domain;

public class CustomerTests
{
    private static readonly DateTime Now = TestData.Now;

    [Test]
    public void Create_TrimsTextAndSetsAllFields()
    {
        var customer = Customer.Create("  Ana ", " Pérez ", new DateOnly(1990, 1, 1), " 001-A ", Gender.F, 1500m, Now);

        Assert.Multiple(() =>
        {
            Assert.That(customer.FirstName, Is.EqualTo("Ana"));
            Assert.That(customer.LastName, Is.EqualTo("Pérez"));
            Assert.That(customer.DocumentNumber, Is.EqualTo("001-A"));
            Assert.That(customer.Gender, Is.EqualTo(Gender.F));
            Assert.That(customer.MonthlyIncome, Is.EqualTo(1500m));
            Assert.That(customer.CreatedAt, Is.EqualTo(Now));
            Assert.That(customer.FullName, Is.EqualTo("Ana Pérez"));
        });
    }

    [Test]
    public void Create_TreatsBlankLastNameAsNull()
    {
        var customer = Customer.Create("Ana", "   ", new DateOnly(1990, 1, 1), "001", Gender.F, 0m, Now);

        Assert.Multiple(() =>
        {
            Assert.That(customer.LastName, Is.Null);
            Assert.That(customer.FullName, Is.EqualTo("Ana"));
        });
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Create_RequiresFirstName(string firstName)
        => Assert.Throws<DomainException>(() => Customer.Create(firstName, null, new DateOnly(1990, 1, 1), "001", Gender.M, 0m, Now));

    [TestCase("")]
    [TestCase("  ")]
    public void Create_RequiresDocumentNumber(string documentNumber)
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), documentNumber, Gender.M, 0m, Now));

    [Test]
    public void Create_RejectsNamesLongerThanLimit()
    {
        var tooLong = new string('a', Customer.MaxNameLength + 1);
        Assert.Throws<DomainException>(() => Customer.Create(tooLong, null, new DateOnly(1990, 1, 1), "001", Gender.M, 0m, Now));
    }

    [Test]
    public void Create_RejectsBirthDateInTheFuture()
    {
        var tomorrow = DateOnly.FromDateTime(Now).AddDays(1);
        Assert.Throws<DomainException>(() => Customer.Create("Ana", null, tomorrow, "001", Gender.M, 0m, Now));
    }

    [Test]
    public void Create_AcceptsBirthDateToday()
    {
        var today = DateOnly.FromDateTime(Now);
        Assert.DoesNotThrow(() => Customer.Create("Ana", null, today, "001", Gender.M, 0m, Now));
    }

    [Test]
    public void Create_RejectsNegativeIncome()
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), "001", Gender.M, -1m, Now));

    [Test]
    public void Create_RejectsUndefinedGender()
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), "001", (Gender)42, 0m, Now));
}
