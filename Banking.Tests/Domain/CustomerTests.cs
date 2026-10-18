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
        var customer = Customer.Create("  Ana ", " Pérez ", new DateOnly(1990, 1, 1), " 001-A ", Gender.F, 1500m, TestData.Password, TestData.Hasher, Now);

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
        var customer = Customer.Create("Ana", "   ", new DateOnly(1990, 1, 1), "001", Gender.F, 0m, TestData.Password, TestData.Hasher, Now);

        Assert.Multiple(() =>
        {
            Assert.That(customer.LastName, Is.Null);
            Assert.That(customer.FullName, Is.EqualTo("Ana"));
        });
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Create_RequiresFirstName(string firstName)
        => Assert.Throws<DomainException>(() => Customer.Create(firstName, null, new DateOnly(1990, 1, 1), "001", Gender.M, 0m, TestData.Password, TestData.Hasher, Now));

    [TestCase("")]
    [TestCase("  ")]
    public void Create_RequiresDocumentNumber(string documentNumber)
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), documentNumber, Gender.M, 0m, TestData.Password, TestData.Hasher, Now));

    [Test]
    public void Create_RejectsNamesLongerThanLimit()
    {
        var tooLong = new string('a', Customer.MaxNameLength + 1);
        Assert.Throws<DomainException>(() => Customer.Create(tooLong, null, new DateOnly(1990, 1, 1), "001", Gender.M, 0m, TestData.Password, TestData.Hasher, Now));
    }

    [Test]
    public void Create_RejectsBirthDateInTheFuture()
    {
        var tomorrow = DateOnly.FromDateTime(Now).AddDays(1);
        Assert.Throws<DomainException>(() => Customer.Create("Ana", null, tomorrow, "001", Gender.M, 0m, TestData.Password, TestData.Hasher, Now));
    }

    [Test]
    public void Create_AcceptsBirthDateToday()
    {
        var today = DateOnly.FromDateTime(Now);
        Assert.DoesNotThrow(() => Customer.Create("Ana", null, today, "001", Gender.M, 0m, TestData.Password, TestData.Hasher, Now));
    }

    [Test]
    public void Create_RejectsNegativeIncome()
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), "001", Gender.M, -1m, TestData.Password, TestData.Hasher, Now));

    [Test]
    public void Create_RejectsUndefinedGender()
        => Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(1990, 1, 1), "001", (Gender)42, 0m, TestData.Password, TestData.Hasher, Now));

    [Test]
    public void Create_StoresOnlyTheHashOfThePassword()
    {
        var customer = TestData.Customer(password: "Correct horse battery");

        Assert.Multiple(() =>
        {
            Assert.That(customer.PasswordHash, Is.Not.Empty.And.Not.EqualTo("Correct horse battery"));
            Assert.That(customer.VerifyPassword("Correct horse battery", TestData.Hasher), Is.True);
            Assert.That(customer.VerifyPassword("correct horse battery", TestData.Hasher), Is.False, "passwords are case-sensitive");
            Assert.That(customer.VerifyPassword(string.Empty, TestData.Hasher), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RequiresPassword(string password)
    {
        var ex = Assert.Throws<DomainException>(() => TestData.Customer(password: password));

        Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.PasswordRequired));
    }

    [Test]
    public void Create_RejectsPasswordShorterThanMinimum()
    {
        var ex = Assert.Throws<DomainException>(() => TestData.Customer(password: new string('x', Customer.MinPasswordLength - 1)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.PasswordTooShort));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { Customer.MinPasswordLength }));
        });
    }

    [Test]
    public void Create_RejectsPasswordLongerThanMaximum()
    {
        var ex = Assert.Throws<DomainException>(() => TestData.Customer(password: new string('x', Customer.MaxPasswordLength + 1)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.PasswordTooLong));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { Customer.MaxPasswordLength }));
        });
    }

    [Test]
    public void Create_AcceptsPasswordAtTheLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => TestData.Customer(password: new string('x', Customer.MinPasswordLength)));
            Assert.DoesNotThrow(() => TestData.Customer(password: new string('x', Customer.MaxPasswordLength)));
        });
    }

    [Test]
    public void Create_DoesNotTrimThePassword()
    {
        var customer = TestData.Customer(password: " padded password ");

        Assert.Multiple(() =>
        {
            Assert.That(customer.VerifyPassword(" padded password ", TestData.Hasher), Is.True);
            Assert.That(customer.VerifyPassword("padded password", TestData.Hasher), Is.False);
        });
    }
}
