using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.EventExpertSer;
using Services.Request.ExpertRatingReq;
using Services.Response.EventResp;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/event-expert")]
    // [Authorize(Roles = "Expert")]
    public class EventExpertController : ControllerBase
    {
        private readonly IEventExpertService _eventExpertService;

        public EventExpertController(IEventExpertService eventExpertService)
        {
            _eventExpertService = eventExpertService;
        }

        #region Actions (Post)

        /// <summary>
        /// CHỦ EVENT: Mời danh sách Expert khác tham gia Hội đồng chấm điểm.
        /// </summary>
        [HttpPost("invite/{eventId}")]
        public async Task<IActionResult> InviteExperts(int eventId, [FromBody] List<int> expertIds)
        {
            try
            {
                if (expertIds == null || expertIds.Count == 0)
                    return BadRequest(new { message = "Danh sách Expert mời không được để trống." });

                var result = await _eventExpertService.InviteExpertsAsync(eventId, expertIds);

                if (result)
                    return Ok(new { message = "Gửi lời mời thành công." });

                return BadRequest(new { message = "Không có lời mời mới nào được gửi (có thể họ đã được mời hoặc là chính bạn)." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// EXPERT: Phản hồi lời mời (Chấp nhận hoặc Từ chối).
        /// </summary>
        [HttpPost("respond/{eventId}")]
        public async Task<IActionResult> RespondToInvitation(int eventId, [FromQuery] bool accept)
        {
            try
            {
                var result = await _eventExpertService.RespondToInvitationAsync(eventId, accept);

                if (result)
                {
                    string status = accept ? "chấp nhận" : "từ chối";
                    return Ok(new { message = $"Bạn đã {status} lời mời tham gia sự kiện." });
                }

                return BadRequest(new { message = "Cập nhật trạng thái lời mời thất bại." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Queries (Get)

        /// <summary>
        /// EXPERT: Lấy danh sách LỜI MỜI MỚI (Trạng thái Pending).
        /// Dùng cho màn hình Thông báo hoặc Hộp thư đến.
        /// </summary>
        [HttpGet("my-invitations")]
        public async Task<ActionResult<IEnumerable<EventListDto>>> GetMyInvitations()
        {
            try
            {
                // Gọi hàm đã refactor trả về DTO hoàn chỉnh (có ảnh, có giải thưởng)
                var invitations = await _eventExpertService.GetPendingInvitationsAsync();
                return Ok(invitations);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// EXPERT: Lấy danh sách SỰ KIỆN ĐANG THAM GIA (Trạng thái Accepted).
        /// Dùng để Expert vào xem các sự kiện mình cần chấm điểm.
        /// </summary>
        [HttpGet("my-assigned-events")]
        public async Task<ActionResult<IEnumerable<EventListDto>>> GetMyAssignedEvents()
        {
            try
            {
                var events = await _eventExpertService.GetEventsInvitedToRateAsync();
                return Ok(events);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        #endregion

        [HttpPost("rate-post")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> RatePost([FromBody] ExpertRatingRequest dto)
        {
            try
            {
                await _eventExpertService.SubmitExpertRatingAsync(dto);
                return Ok(new { message = "Chấm điểm thành công và đã cập nhật bảng xếp hạng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("review-submissions/{eventId}")]
        [Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetSubmissions(int eventId)
        {
            var posts = await _eventExpertService.GetPostsForReviewAsync(eventId);
            return Ok(posts);
        }
    }
}