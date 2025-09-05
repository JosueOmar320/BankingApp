using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;
        
        public BankAccountController(IBankAccountService bankAccountService)
        {
            _bankAccountService = bankAccountService;
        }

        /// <summary>
        /// Create a new Bank Account.
        /// </summary>
        /// <param name="bankAccountDto">DTO with the bankAccount's data to be created</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        [HttpPost]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> CreateBankAccount(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _bankAccountService.CreateBankAccountAsync(bankAccountDto, cancellationToken);

            return Created("", result);
        }

        /// <summary>
        /// Get balance by Account Number.
        /// </summary>
        /// <param name="accountNumber">Customer's Account Number</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        [HttpGet("{accountNumber}/balance")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> GetBalanceByAccountNumber(string accountNumber, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _bankAccountService.GetBalanceByAccountNumberAsync(accountNumber, cancellationToken);

            if(result == null)
                return NotFound(new ErrorResponseDto { Message = $"Account {accountNumber} not found." });

            return Ok(result);
        }
    }
}
