using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Create a new customer.
        /// </summary>
        /// <param name="customer">DTO with the customer's data to be created</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _customerService.CreateCustomerAsync(customer, cancellationToken);

            return Created("", result);
        }
    }
}
