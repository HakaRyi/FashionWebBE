using Microsoft.AspNetCore.Mvc;
using Services.Implements.ExpertRatingImp;
using Services.Request.ExpertRatingReq;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/expert-rating")]
    public class ExpertRatingController : ControllerBase
    {
        private readonly IExpertRatingService _eventRatingService;


        public ExpertRatingController(IExpertRatingService eventRatingService)
        {
            _eventRatingService = eventRatingService;
        }

        /// <summary>
        /// Chuyên gia chấm điểm cho bài thi trong sự kiện
        /// </summary>
        [HttpPost("submit-rating")]
        public async Task<IActionResult> SubmitRating([FromBody] ExpertRatingRequest request)
        {
            try
            {
                await _eventRatingService.SubmitExpertRatingAsync(request);
                return Ok(new { message = "Chấm điểm thành công và đã cập nhật bảng điểm tổng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
