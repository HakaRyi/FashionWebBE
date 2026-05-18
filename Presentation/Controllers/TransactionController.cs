using Application.Interfaces;
using Application.Response.TransactionResp;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Presentation.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IWhaleService _whaleService;
        public TransactionController(ITransactionService transactionService, IWhaleService whaleService)
        {
            _transactionService = transactionService;
            _whaleService = whaleService;
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

        [HttpGet("feature-intelligence-dashboard")]
        [ProducesResponseType(typeof(FeatureIntelligenceResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFeatureIntelligenceDashboard([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Truyền các tham số ngày chọn từ Query String xuống Service
                var result = await _transactionService.GetFeatureIntelligenceDashboardAsync(startDate, endDate);

                if (result == null)
                {
                    return NotFound(new { message = "Không thể khởi tạo hoặc tìm thấy dữ liệu thống kê." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Gợi ý: Nên inject ILogger vào Controller để log lỗi chi tiết thay vì chỉ dùng Console
                // _logger.LogError(ex, "Đã xảy ra lỗi khi lấy dữ liệu Feature Intelligence Dashboard.");

                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi hệ thống khi xử lý dữ liệu Dashboard.",
                    details = ex.Message
                });
            }
        }

        [HttpGet("whales")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WhaleDashboardDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<WhaleDashboardDto>>> GetTopWhales(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? viewMode,
            [FromQuery] string? searchQuery = null)
        {
            // 1. Thiết lập dải ngày mặc định nếu Frontend không truyền lên (Mặc định lấy 30 ngày gần nhất)
            var end = toDate ?? DateTime.Now;
            var start = fromDate ?? end.AddDays(-29);

            // 2. Validate dải ngày hợp lệ
            if (start > end)
            {
                return BadRequest(new { Message = "Ngày bắt đầu (fromDate) không thể lớn hơn ngày kết thúc (toDate)." });
            }

            // 3. Gọi Service xử lý toàn bộ logic nghiệp vụ (Tính toán LTV, Khấu trừ Refund, Chia phân đoạn Trend)
            var result = await _whaleService.GetTopWhalesAsync(start, end, viewMode, searchQuery);

            return Ok(result);
        }

        [HttpGet("whale/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WhaleHistoryDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<WhaleHistoryDto>> GetWhaleHistory(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { Message = "The ID cannot be left blank." });
            }

            // Sàng lọc dữ liệu: Nếu bắt đầu bằng "W-" hoặc "w-" thì cắt bỏ, nếu không thì giữ nguyên để ép kiểu số
            string cleanId = id.Trim();
            if (cleanId.StartsWith("W-", StringComparison.OrdinalIgnoreCase))
            {
                cleanId = cleanId.Substring(2); // Cắt bỏ 2 ký tự đầu
            }

            // Ép kiểu sang int, nếu thất bại chứng tỏ chuỗi truyền lên chứa ký tự lạ (Ví dụ: "W-abc" hoặc "xyz")
            if (!int.TryParse(cleanId, out int walletId))
            {
                return BadRequest(new { Message = $"The identifier '{id}' is invalid. The system only accepts pure numbers (e.g., 12) or the 'W-12' structure." });
            }

            // 2. Gọi Service xử lý dữ liệu thô với ID dạng int chuẩn
            var historyDto = await _whaleService.GetWhaleHistoryAsync(walletId);

            if (historyDto == null)
            {
                return NotFound(new { Message = $"No data or transaction history was found for customer ID: {id}." });
            }

            return Ok(historyDto);
        }

        [HttpGet("shops")]
        public async Task<IActionResult> GetDashboardData([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                // Nếu FE không truyền ngày, mặc định lấy trong vòng 30 ngày qua
                DateTime finalFromDate = fromDate ?? DateTime.Now.AddDays(-30);
                DateTime finalToDate = toDate ?? DateTime.Now;

                if (finalFromDate > finalToDate)
                {
                    return BadRequest(new { message = "Ngày bắt đầu không được lớn hơn ngày kết thúc." });
                }

                // --- CÁCH 1: TÍNH TOÁN THỜI GIAN KỲ TRƯỚC ---
                var duration = finalToDate - finalFromDate;
                // Lùi ngày bắt đầu về trước một khoảng thời gian tương đương duration để lấy dữ liệu đối chiếu
                DateTime previousFromDate = finalFromDate.Subtract(duration);

                // Truyền thêm ngày bắt đầu của kỳ trước vào Service
                var data = await _transactionService.GetRankingManagementDashboardAsync(previousFromDate, finalFromDate, finalToDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
