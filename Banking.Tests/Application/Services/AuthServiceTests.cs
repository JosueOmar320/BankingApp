using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Application.Interfaces;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Banking.Tests.Application.Services;

public class AuthServiceTests
{
    private static readonly AccessToken IssuedToken = new("header.payload.signature", TestData.Now.AddHours(1));

    private Mock<ICustomerRepository> _customers = null!;
    private Mock<IAccessTokenIssuer> _issuer = null!;
    private FakePasswordHasher _hasher = null!;
    private AuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        _issuer = new Mock<IAccessTokenIssuer>(MockBehavior.Strict);
        _hasher = new FakePasswordHasher();
        _service = new AuthService(_customers.Object, _hasher, _issuer.Object, NullLogger<AuthService>.Instance);
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsTheIssuedToken()
    {
        var customer = TestData.Customer("DOC-1");
        _customers.Setup(c => c.GetCustomerByDocumentNumberAsync("DOC-1", It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _issuer.Setup(i => i.Issue(customer)).Returns(IssuedToken);

        var result = await _service.LoginAsync(new LoginRequestDto { DocumentNumber = "DOC-1", Password = TestData.Password });

        Assert.That(result, Is.EqualTo(new LoginResponseDto(IssuedToken.Value, "Bearer", IssuedToken.ExpiresAtUtc, customer.CustomerId)));
    }

    [Test]
    public async Task LoginAsync_TrimsTheDocumentNumber_LikeRegistrationDoes()
    {
        var customer = TestData.Customer("DOC-1");
        _customers.Setup(c => c.GetCustomerByDocumentNumberAsync("DOC-1", It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _issuer.Setup(i => i.Issue(customer)).Returns(IssuedToken);

        var result = await _service.LoginAsync(new LoginRequestDto { DocumentNumber = "  DOC-1 ", Password = TestData.Password });

        Assert.That(result.AccessToken, Is.EqualTo(IssuedToken.Value));
    }

    [Test]
    public void LoginAsync_WithWrongPassword_ThrowsInvalidCredentials_AndIssuesNothing()
    {
        _customers.Setup(c => c.GetCustomerByDocumentNumberAsync("DOC-1", It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Customer("DOC-1"));

        var ex = Assert.ThrowsAsync<InvalidCredentialsException>(() => _service.LoginAsync(new LoginRequestDto { DocumentNumber = "DOC-1", Password = "wrong password" }));

        Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.InvalidCredentials));
        _issuer.Verify(i => i.Issue(It.IsAny<Customer>()), Times.Never);
    }

    [Test]
    public void LoginAsync_WithUnknownDocument_ThrowsTheSameError_AndStillHashes()
    {
        _customers.Setup(c => c.GetCustomerByDocumentNumberAsync("NOBODY", It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var ex = Assert.ThrowsAsync<InvalidCredentialsException>(() => _service.LoginAsync(new LoginRequestDto { DocumentNumber = "NOBODY", Password = TestData.Password }));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(ApplicationErrorCodes.InvalidCredentials));
            Assert.That(ex.Message, Does.Not.Contain("NOBODY"), "the message never says which part of the credentials failed");
            Assert.That(_hasher.HashCalls, Is.EqualTo(1), "an unknown document costs a hash too, so timing does not reveal registered documents");
        });
        _issuer.Verify(i => i.Issue(It.IsAny<Customer>()), Times.Never);
    }
}
