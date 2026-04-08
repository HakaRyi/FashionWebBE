using Application.Request.TryOn;
using Application.Services.TryOn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TryOnHistoryController : ControllerBase
    {
        private readonly ITryOnHistoryService _historyService;

        public TryOnHistoryController(ITryOnHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpPost("save")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveHistory([FromForm] CreateHistoryTryOnRequest request)
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrWhiteSpace(accountIdClaim))
                throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var accountId = int.Parse(accountIdClaim);

            var tryOnId = await _historyService.CreateTryOnHistoryAsync(accountId, request);

            return Ok(new
            {
                success = true,
                message = "Lưu lịch sử thử đồ thành công.",
                data = new
                {
                    tryOnId
                }
            });
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrWhiteSpace(accountIdClaim))
                throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var accountId = int.Parse(accountIdClaim);

            var result = await _historyService.GetTryOnHistoryByAccountIdAsync(accountId);

            return Ok(new
            {
                success = true,
                message = "Lấy lịch sử thử đồ thành công.",
                data = result
            });
        }

        [HttpDelete("{tryOnId:int}")]
        public async Task<IActionResult> DeleteHistory(int tryOnId)
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrWhiteSpace(accountIdClaim))
                throw new UnauthorizedAccessException("Không xác định được người dùng.");

            var accountId = int.Parse(accountIdClaim);

            await _historyService.DeleteTryOnHistoryAsync(accountId, tryOnId);

            return Ok(new
            {
                success = true,
                message = "Xóa lịch sử thử đồ thành công."
            });
        }
    }
}