using Application.Interfaces;
using Application.Services.RecommendationImp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var result = await _recommendationService.GetMyHistoryAsync();
            return Ok(new { message = "Lấy lịch sử gợi ý thành công.", data = result });
        }

        [HttpGet("history/{historyId:int}")]
        public async Task<IActionResult> GetHistoryDetail(int historyId)
        {
            var result = await _recommendationService.GetHistoryDetailsAsync(historyId);

            if (result == null || result.Count == 0)
                return NotFound(new { message = "Không tìm thấy chi tiết lịch sử hoặc bạn không có quyền xem." });

            return Ok(new { message = "Lấy chi tiết gợi ý thành công.", data = result });
        }
    }
}
