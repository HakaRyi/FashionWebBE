using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _transactionService.GetTransactions();

            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    message = "No transactions found."
                });
            }

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var result = await _transactionService.GetById(id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Transaction not found."
                });
            }

            return Ok(result);
        }

        [HttpPost("admin/request-fix")]
        public async Task<IActionResult> RequestFix(
            [FromQuery] int escrowId,
            [FromQuery] string reason)
        {
            await _transactionService.AdminRequestFixLeakAsync(escrowId, reason);

            return Ok(new
            {
                message = "Request fix sent to expert successfully."
            });
        }

        [HttpPost("expert/approve-fix/{escrowId:int}")]
        public async Task<IActionResult> ExpertApprove([FromRoute] int escrowId)
        {
            await _transactionService.ExpertApproveFixAsync(escrowId);

            return Ok(new
            {
                message = "Expert approved the fix request successfully."
            });
        }

        [HttpPost("admin/execute-fix/{escrowId:int}")]
        public async Task<IActionResult> ExecuteFix([FromRoute] int escrowId)
        {
            await _transactionService.AdminExecuteUpdateWalletAsync(escrowId);

            return Ok(new
            {
                message = "Wallet update executed successfully."
            });
        }

        [HttpGet("admin/escrow-management")]
        public async Task<IActionResult> GetEscrowManagement()
        {
            var result = await _transactionService.AdminGetEscrowManagementAsync();
            return Ok(result);
        }

        [HttpGet("expert/escrow-management")]
        public async Task<IActionResult> GetExpertEscrow()
        {
            var result = await _transactionService.ExpertGetEscrowManagementAsync();
            return Ok(result);
        }

        [HttpGet("history/wallet")]
        public async Task<IActionResult> GetWalletHistory()
        {
            var result = await _transactionService.ExpertGetHistoryAsync();
            return Ok(result);
        }

        [HttpGet("by-reference")]
        public async Task<IActionResult> GetByRef(
            [FromQuery] string refType,
            [FromQuery] int refId)
        {
            var result = await _transactionService.GetTransactionsByReferenceAsync(refType, refId);
            return Ok(result);
        }

        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllAdmin(
            [FromQuery] string? type = null,
            [FromQuery] string? refType = null,
            [FromQuery] int? refId = null,
            [FromQuery] string? search = null,
            [FromQuery] string? searchBy = null)
        {
            var result = await _transactionService.AdminGetAllTransactionsAsync(
                type,
                refType,
                refId,
                search,
                searchBy
            );

            return Ok(result);
        }
    }
}