using Application.Interfaces;
using Application.Services.PostImp;
using Microsoft.AspNetCore.Mvc;
using Application.Request.ExpertRatingReq;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/expert-rating")]
    public class ExpertRatingController : ControllerBase
    {
        private readonly IExpertRatingService _eventRatingService;
        private readonly IPostService _postService;



        public ExpertRatingController(IExpertRatingService eventRatingService, IPostService postService)
        {
            _eventRatingService = eventRatingService;
            _postService = postService;
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

        [HttpGet("my-reviews/{eventId}")]
        //[Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetMyReviews(int eventId)
        {
            var data = await _postService.GetPostsForExpertReviewAsync(eventId);
            return Ok(data);
        }
    }
}
