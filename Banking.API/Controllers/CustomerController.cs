using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

/// <summary>Customer registration (public) and lookup (own record only).</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    /// <summary>Creates the controller.</summary>
    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Registers a new customer. This is the public sign-up: no token needed.</summary>
    /// <remarks>Afterwards, log in with the document number and password at <c>POST /api/Auth/login</c>.</remarks>
    /// <param name="customer">Customer data, including the login password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="201">The customer was created.</response>
    /// <response code="400">The payload is invalid or violates a business rule.</response>
    /// <response code="409">A customer with the same document number already exists.</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
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
}
