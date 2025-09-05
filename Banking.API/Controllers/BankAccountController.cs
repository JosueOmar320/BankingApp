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
        private readonly ITransactionService _transactionService;
        public BankAccountController(IBankAccountService bankAccountService, ITransactionService transactionService)
        {
            _bankAccountService = bankAccountService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// Create a new Bank Account.
        /// </summary>
        /// <param name="bankAccountDto">DTO with the bankAccount's data to be created</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        [HttpPost("CreateBankAccount")]
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
        /// Create a new Bank Account.
        /// </summary>
        /// <param name="accountNumber">Customer's Account Number</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        [HttpGet("GetBalanceByAccountNumber")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> GetBalanceByAccountNumber(string accountNumber, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return StatusCode(StatusCodes.Status499ClientClosedRequest);

            var result = await _bankAccountService.GetBalanceByAccountNumberAsync(accountNumber, cancellationToken);

            if(result == null)
                return NotFound($"Account {accountNumber} not found.");

            return Ok(result);
        }

        /// <summary>
        /// Deposito Amount in BankAccount.
        /// </summary>
        /// <param name="accountNumber">Customer's Account Number</param>
        /// <param name="amount">Amount to deposit</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated bank account information.</returns>
        [HttpPost("Transactions/Deposit")]
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
        [HttpPost("Transactions/Withdraw")]
        [ProducesResponseType(typeof(BankAccountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> Withdraw(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
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
        [HttpPost("Transactions/Apply-Interest")]
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
        [HttpGet("Transactions/Summary")]
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
