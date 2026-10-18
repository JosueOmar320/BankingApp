using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

/// <summary>Bank account opening and balance queries.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BankAccountController : ControllerBase
{
    private readonly IBankAccountService _bankAccountService;

    /// <summary>Creates the controller.</summary>
    public BankAccountController(IBankAccountService bankAccountService)
    {
        _bankAccountService = bankAccountService;
    }

    /// <summary>Opens a bank account for an existing customer.</summary>
    /// <remarks>The opening deposit must be at least 10.00 and is recorded as the first transaction.</remarks>
    /// <param name="bankAccountDto">Owner and opening deposit.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="201">The account was opened.</response>
    /// <response code="400">The payload is invalid or the opening deposit violates a business rule.</response>
    /// <response code="404">The customer does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankAccountResponseDto>> CreateBankAccount(CreateBankAccountDto bankAccountDto, CancellationToken cancellationToken)
    {
        var created = await _bankAccountService.CreateBankAccountAsync(bankAccountDto, cancellationToken);
        return CreatedAtAction(nameof(GetByAccountNumber), new { accountNumber = created.AccountNumber }, created);
    }

    /// <summary>Gets an account by its number.</summary>
    /// <param name="accountNumber">Account number.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The account.</response>
    /// <response code="404">The account does not exist.</response>
    [HttpGet("{accountNumber}")]
    [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankAccountResponseDto>> GetByAccountNumber(string accountNumber, CancellationToken cancellationToken)
        => Ok(await _bankAccountService.GetByAccountNumberAsync(accountNumber, cancellationToken));

    /// <summary>Gets the balance of an account.</summary>
    /// <param name="accountNumber">Account number.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The balance.</response>
    /// <response code="404">The account does not exist.</response>
    [HttpGet("{accountNumber}/balance")]
    [ProducesResponseType(typeof(AccountBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountBalanceDto>> GetBalanceByAccountNumber(string accountNumber, CancellationToken cancellationToken)
        => Ok(await _bankAccountService.GetBalanceByAccountNumberAsync(accountNumber, cancellationToken));
}
