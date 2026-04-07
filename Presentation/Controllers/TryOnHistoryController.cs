using Application.Services.TryOn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Request.TryOn;

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
            try
            {
                var accountId = int.Parse(User.FindFirst("AccountId")?.Value!);
                var result = await _historyService.CreateTryOnHistoryAsync(accountId, request);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            try
            {
                var accountId = int.Parse(User.FindFirst("AccountId")?.Value!);
                var result = await _historyService.GetTryOnHistoryByAccountIdAsync(accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
