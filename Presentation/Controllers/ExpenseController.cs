using Application.Request.WalletReq;
using Application.Services.WalletImp;
using Application.Utils;
using Domain.Dto.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/expenses")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet("me/transactions")]
        public async Task<IActionResult> GetMyTransactions([FromQuery] TransactionFilterRequestDto request)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetMyTransactionsAsync(accountId, request);
            return Ok(result);
        }

        [HttpGet("me/transactions/{transactionId:int}")]
        public async Task<IActionResult> GetTransactionDetail(int transactionId)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetTransactionDetailAsync(accountId, transactionId);
            return Ok(result);
        }

        [HttpGet("me/expense-summary")]
        public async Task<IActionResult> GetMyExpenseSummary([FromQuery] ExpenseSummaryRequestDto request)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetMyExpenseSummaryAsync(accountId, request);
            return Ok(result);
        }

        [HttpGet("me/expense-by-reference-type")]
        public async Task<IActionResult> GetExpenseByReferenceType([FromQuery] ExpenseByReferenceTypeRequestDto request)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetExpenseByReferenceTypeAsync(accountId, request);
            return Ok(result);
        }

        [HttpGet("me/cashflow")]
        public async Task<IActionResult> GetCashflow([FromQuery] CashflowRequestDto request)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetCashflowAsync(accountId, request);
            return Ok(result);
        }

        [HttpGet("me/spending-limit")]
        public async Task<IActionResult> GetMySpendingLimit([FromQuery] int month, [FromQuery] int year)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.GetMySpendingLimitAsync(accountId, month, year);
            return Ok(result);
        }

        [HttpPut("me/spending-limit")]
        public async Task<IActionResult> UpdateMySpendingLimit([FromBody] UpdateSpendingLimitRequestDto request)
        {
            var accountId = User.GetUserId();
            var result = await _expenseService.UpdateMySpendingLimitAsync(accountId, request);
            return Ok(result);
        }
    }
}