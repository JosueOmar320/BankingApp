using Banking.API.RateLimiting;
using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banking.API.Controllers;

/// <summary>Customer registration (public), lookup and account listing (own record only).</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IBankAccountService _bankAccountService;

    /// <summary>Creates the controller.</summary>
    public CustomerController(ICustomerService customerService, IBankAccountService bankAccountService)
    {
        _customerService = customerService;
        _bankAccountService = bankAccountService;
    }

    /// <summary>Registers a new customer. This is the public sign-up: no token needed.</summary>
    /// <remarks>Afterwards, log in with the document number and password at <c>POST /api/Auth/login</c>.</remarks>
    /// <param name="customer">Customer data, including the login password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="201">The customer was created.</response>
    /// <response code="400">The payload is invalid or violates a business rule.</response>
    /// <response code="409">A customer with the same document number already exists.</response>
    /// <response code="429">Too many registrations from this address; wait for the <c>Retry-After</c> seconds.</response>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerResponseDto>> CreateCustomer(CreateCustomerDto customer, CancellationToken cancellationToken)
    {
        var created = await _customerService.CreateCustomerAsync(customer, cancellationToken);
        return CreatedAtAction(nameof(GetCustomerById), new { customerId = created.CustomerId }, created);
    }

    /// <summary>Gets a customer by id. Only the authenticated customer's own record is accessible.</summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The customer.</response>
    /// <response code="404">The customer does not exist.</response>
    [HttpGet("{customerId:int}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponseDto>> GetCustomerById(int customerId, CancellationToken cancellationToken)
        => Ok(await _customerService.GetCustomerByIdAsync(customerId, cancellationToken));

    /// <summary>Lists the accounts of a customer, oldest first. Only the authenticated customer's own accounts are accessible.</summary>
    /// <remarks>
    /// Accounts are a sub-collection of the customer, so the route hangs off <c>/api/Customer/{customerId}</c>
    /// like the customer record itself; <c>/api/BankAccount</c> routes address a single account by its number.
    /// A customer without accounts gets an empty list.
    /// </remarks>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The customer's accounts, oldest first.</response>
    /// <response code="404">The customer does not exist.</response>
    [HttpGet("{customerId:int}/accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<BankAccountResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BankAccountResponseDto>>> GetAccounts(int customerId, CancellationToken cancellationToken)
        => Ok(await _bankAccountService.GetByCustomerIdAsync(customerId, cancellationToken));
}
