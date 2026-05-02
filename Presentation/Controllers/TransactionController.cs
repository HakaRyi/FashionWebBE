using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
        // GET: api/<TransactionController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _transactionService.GetTransactions();
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "No transactions found"
                });
            }

        }

        // GET api/<TransactionController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var result = await _transactionService.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(404, new
                {
                    message = "Transaction not found"
                });
            }
        }

        // --- NHÓM XỬ LÝ KẸT TIỀN (FLOW 3 BƯỚC) ---

        [HttpPost("admin/request-fix")]
        public async Task<IActionResult> RequestFix([FromQuery] int escrowId, [FromQuery] string reason)
        {
            await _transactionService.AdminRequestFixLeakAsync(escrowId, reason);
            return Ok(new { message = "Gửi yêu cầu tới Expert thành công" });
        }

        [HttpPost("expert/approve-fix/{escrowId}")]
        public async Task<IActionResult> ExpertApprove(int escrowId)
        {
            await _transactionService.ExpertApproveFixAsync(escrowId);
            return Ok(new { message = "Expert đã phê duyệt" });
        }

        [HttpPost("admin/execute-fix/{escrowId}")]
        public async Task<IActionResult> ExecuteFix(int escrowId)
        {
            await _transactionService.AdminExecuteUpdateWalletAsync(escrowId);
            return Ok(new { message = "Đã thực thi cập nhật ví thành công" });
        }

        // --- NHÓM QUẢN LÝ & TRA CỨU ---

        // Hàm 4: Quản lý Escrow (Các phiên giữ tiền)
        [HttpGet("admin/escrow-management")]
        public async Task<IActionResult> GetEscrowManagement()
        {
            var result = await _transactionService.AdminGetEscrowManagementAsync();
            return Ok(result);
        }

        [HttpGet("expert/escrow-management")]
        // [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetExpertEscrow()
        {
            try
            {
                var result = await _transactionService.ExpertGetEscrowManagementAsync();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        // Hàm 5: Lịch sử theo ví
        [HttpGet("history/wallet")]
        public async Task<IActionResult> GetWalletHistory()
        {
            try
            {
                var result = await _transactionService.ExpertGetHistoryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log ex.Message ra đây để xem lỗi thật sự là gì
                return BadRequest(new { message = ex.Message });
            }
        }

        // Hàm 6: Tra cứu theo Reference (Dùng cho đối soát sự kiện/đơn hàng)
        [HttpGet("by-reference")]
        public async Task<IActionResult> GetByRef([FromQuery] string refType, [FromQuery] int refId)
        {
            var result = await _transactionService.GetTransactionsByReferenceAsync(refType, refId);
            return Ok(result);
        }

        // Hàm 7: Tổng tra cứu cho Admin (Có filter linh hoạt)
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllAdmin([FromQuery] string? type, [FromQuery] string? refType, [FromQuery] int? refId)
        {
            var result = await _transactionService.AdminGetAllTransactionsAsync(type, refType, refId);
            return Ok(result);
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
