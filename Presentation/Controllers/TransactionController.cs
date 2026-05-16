using Application.Interfaces;
using Application.Response.TransactionResp;
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

        [HttpGet("feature-intelligence-dashboard")]
        [ProducesResponseType(typeof(FeatureIntelligenceResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFeatureIntelligenceDashboard()
        {
            try
            {
                var result = await _transactionService.GetFeatureIntelligenceDashboardAsync();

                if (result == null)
                {
                    return NotFound(new { message = "Không thể khởi tạo hoặc tìm thấy dữ liệu thống kê." });
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                // Thay bằng LogError của ILogger nếu dự án của bạn có setup log định dạng
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi hệ thống khi xử lý dữ liệu Dashboard.",
                    details = ex.Message
                });
            }
        }
        // POST api/<TransactionController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<TransactionController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<TransactionController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}