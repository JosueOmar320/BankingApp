using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
using Banking.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banking.Tests.Application.Services;

public class CustomerServiceTests
{
    private Mock<ICustomerRepository> _repository = null!;
    private FakeCurrentCustomer _caller = null!;
    private CustomerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _caller = new FakeCurrentCustomer(5);
        _service = new CustomerService(_repository.Object, TestData.Hasher, _caller, new FixedTimeProvider(), NullLogger<CustomerService>.Instance);
    }

    [Test]
    public async Task CreateCustomerAsync_BuildsEntityFromDtoAndReturnsSavedData()
    {
        var dto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            BirthDate = new DateOnly(1990, 1, 1),
            DocumentNumber = "123456789",
            Gender = Gender.M,
            MonthlyIncome = 2000m,
            Password = "Plain text pw 1",
        };

        Customer? captured = null;
        _repository
            .Setup(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var result = await _service.CreateCustomerAsync(dto);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.FirstName, Is.EqualTo("John"));
            Assert.That(captured.DocumentNumber, Is.EqualTo("123456789"));
            Assert.That(captured.CreatedAt, Is.EqualTo(TestData.Now));
            Assert.That(captured.PasswordHash, Is.EqualTo(FakePasswordHasher.Prefix + "Plain text pw 1"), "only the hash is stored");
            Assert.That(result.FirstName, Is.EqualTo("John"));
            Assert.That(result.LastName, Is.EqualTo("Doe"));
            Assert.That(result.Gender, Is.EqualTo(Gender.M));
            Assert.That(result.MonthlyIncome, Is.EqualTo(2000m));
            Assert.That(result.CreatedAt, Is.EqualTo(TestData.Now));
        });
        _repository.Verify(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CreateCustomerAsync_DoesNotHitRepositoryWhenDomainRulesFail()
    {
        var dto = new CreateCustomerDto
        {
            FirstName = "John",
            BirthDate = new DateOnly(2999, 1, 1),
            DocumentNumber = "123",
            Gender = Gender.M,
            Password = TestData.Password,
        };

        Assert.ThrowsAsync<DomainException>(() => _service.CreateCustomerAsync(dto));
        _repository.Verify(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void CreateCustomerAsync_PropagatesDuplicateDocumentNumber()
    {
        var dto = new CreateCustomerDto { FirstName = "John", BirthDate = new DateOnly(1990, 1, 1), DocumentNumber = "123", Gender = Gender.M, Password = TestData.Password };
        _repository
            .Setup(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateDocumentNumberException());

        Assert.ThrowsAsync<DuplicateDocumentNumberException>(() => _service.CreateCustomerAsync(dto));
    }

    [Test]
    public async Task GetCustomerByIdAsync_ReturnsDto()
    {
        _repository.Setup(r => r.GetCustomerByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());

        var result = await _service.GetCustomerByIdAsync(5);

        Assert.That(result.FirstName, Is.EqualTo("Ana"));
    }

    [Test]
    public void GetCustomerByIdAsync_ThrowsNotFound()
    {
        _repository.Setup(r => r.GetCustomerByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var ex = Assert.ThrowsAsync<NotFoundException>(() => _service.GetCustomerByIdAsync(99));
        Assert.That(ex!.Message, Does.Contain("99"));
    }

    [Test]
    public void GetCustomerByIdAsync_ThrowsForbiddenForAnotherCustomer()
    {
        _repository.Setup(r => r.GetCustomerByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());

        var ex = Assert.ThrowsAsync<ForbiddenException>(() => _service.GetCustomerByIdAsync(7));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.CustomerAccessDenied));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { 7 }));
        });
    }

    [Test]
    public void GetCustomerByIdAsync_ThrowsForbiddenForAnonymousCaller()
    {
        _caller.CustomerId = null;
        _repository.Setup(r => r.GetCustomerByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer());

        Assert.ThrowsAsync<ForbiddenException>(() => _service.GetCustomerByIdAsync(5));
    }
}
