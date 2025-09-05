using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Deposito Amount in BankAccount.
        /// </summary>
        /// <param name="accountNumber">Customer's Account Number</param>
        /// <param name="amount">Amount to deposit</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account information.</returns>
        [HttpPost("Deposit")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> Deposit(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _transactionService.DepositAsync(accountNumber, amount, cancellationToken);

            if (result == null)
                return NotFound($"Account {accountNumber} not found.");

            return Ok(result);
        }

        /// <summary>
        /// Withdraw Amount in BankAccount.
        /// </summary>
        /// <param name="accountNumber">The unique account number of the bank account.</param>
        /// <param name="amount">Amount to Withdraw</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account information.</returns>
        [HttpPost("Withdrawal")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> Withdrawal(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _transactionService.WithdrawAsync(accountNumber, amount, cancellationToken);

            if (result == null)
                return NotFound($"Account {accountNumber} not found.");

            return Ok(result);
        }

        /// <summary>
        /// Apply Interest in BankAccount.
        /// </summary>
        /// <param name="accountNumber">The unique account number of the bank account.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account information.</returns>
        [HttpPost("Apply-Interest")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> ApplyInterest(string accountNumber, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _transactionService.ApplyInterestAsync(accountNumber, cancellationToken);

            if (result == null)
                return NotFound($"Account {accountNumber} not found.");

            return Ok(result);
        }

        /// <summary>
        /// Get the transaction summary of a bank account, including all deposits, withdrawals,
        /// interests applied, and the final balance.
        /// </summary>
        /// <param name="accountNumber">The unique account number of the bank account.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A summary of the transactions for the specified account, or 404 if the account does not exist.</returns>
        [HttpGet("Summary")]
        [ProducesResponseType(typeof(AccountTransactionSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> GetAccountTransactionSummary(string accountNumber, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _transactionService.GetAccountTransactionSummaryAsync(accountNumber, cancellationToken);

            if (result == null)
                return NotFound($"Account {accountNumber} not found.");

            return Ok(result);
        }
    }
}
