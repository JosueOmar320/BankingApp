using Banking.API.RateLimiting;
using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Banking.API.Controllers;

/// <summary>Ledger operations: deposits, withdrawals, interest and account summaries, on the caller's own accounts.</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    /// <summary>Creates the controller.</summary>
    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>Deposits money into an account.</summary>
    /// <param name="request">Account number and amount.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The recorded transaction and the new balance.</response>
    /// <response code="400">The payload is invalid or the amount violates a business rule.</response>
    /// <response code="404">The account does not exist.</response>
    /// <response code="409">The account was modified concurrently; retry the request.</response>
    /// <response code="429">Too many operations in a short time; wait for the <c>Retry-After</c> seconds.</response>
    [HttpPost("Deposit")]
    [EnableRateLimiting(RateLimitPolicies.MoneyOperations)]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TransactionResponseDto>> Deposit(TransactionRequestDto request, CancellationToken cancellationToken)
        => Ok(await _transactionService.DepositAsync(request, cancellationToken));

    /// <summary>Withdraws money from an account.</summary>
    /// <param name="request">Account number and amount.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The recorded transaction and the new balance.</response>
    /// <response code="400">The payload is invalid, the amount violates a business rule or funds are insufficient.</response>
    /// <response code="404">The account does not exist.</response>
    /// <response code="409">The account was modified concurrently; retry the request.</response>
    /// <response code="429">Too many operations in a short time; wait for the <c>Retry-After</c> seconds.</response>
    [HttpPost("Withdrawal")]
    [EnableRateLimiting(RateLimitPolicies.MoneyOperations)]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TransactionResponseDto>> Withdrawal(TransactionRequestDto request, CancellationToken cancellationToken)
        => Ok(await _transactionService.WithdrawAsync(request, cancellationToken));

    /// <summary>Credits the fixed interest rate (10%) on the current balance.</summary>
    /// <param name="request">Account number.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The recorded interest transaction and the new balance.</response>
    /// <response code="400">The resulting interest is zero.</response>
    /// <response code="404">The account does not exist.</response>
    /// <response code="409">The account was modified concurrently; retry the request.</response>
    /// <response code="429">Too many operations in a short time; wait for the <c>Retry-After</c> seconds.</response>
    [HttpPost("Apply-Interest")]
    [EnableRateLimiting(RateLimitPolicies.MoneyOperations)]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TransactionResponseDto>> ApplyInterest(ApplyInterestRequestDto request, CancellationToken cancellationToken)
        => Ok(await _transactionService.ApplyInterestAsync(request, cancellationToken));

    /// <summary>Returns the totals of an account's ledger, the current balance and one page of transactions.</summary>
    /// <remarks>
    /// Totals cover the whole ledger; <c>transactions</c> holds only the requested page (oldest first) and
    /// <c>page</c> says how many pages there are. The default page size is 50 and the maximum 200.
    /// </remarks>
    /// <param name="accountNumber">Account number.</param>
    /// <param name="paging">Page number and size (<c>page</c> and <c>pageSize</c> query parameters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <response code="200">The account summary.</response>
    /// <response code="400">The page number or size is out of range.</response>
    /// <response code="404">The account does not exist.</response>
    // The parameter is not called "page": a query key with the parameter's name makes the binder look for "page.Page".
    [HttpGet("{accountNumber}/summary")]
    [ProducesResponseType(typeof(AccountTransactionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountTransactionSummaryDto>> GetAccountTransactionSummary(string accountNumber, [FromQuery] LedgerPageRequestDto paging, CancellationToken cancellationToken)
        => Ok(await _transactionService.GetAccountTransactionSummaryAsync(accountNumber, paging, cancellationToken));
}
