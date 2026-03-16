using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Auth;
using Services.Implements.Events;
using Services.Request.EventReq;
using Services.Request.ExpertRatingReq;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/events")]
    //[Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ICurrentUserService _currentUserService;

        public EventsController(IEventService eventService, ICurrentUserService currentUserService)
        {
            _eventService = eventService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Tạo sự kiện mới và ký quỹ tiền thưởng từ ví Expert hiện tại
        /// </summary>
        [HttpPost("create-with-prizes")]
        public async Task<IActionResult> CreateEventWithPrizes([FromBody] CreateEventRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _eventService.CreateEventAndLockFundsAsync(request);

                return Ok(new
                {
                    message = "Sự kiện đã được tạo và ký quỹ thành công!",
                    eventId = result.EventId,
                    status = result.Status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách các sự kiện do Expert hiện tại tạo
        /// </summary>
        [HttpGet("my-events")]
        public async Task<IActionResult> GetMyEvents()
        {
            int currentUserId = _currentUserService.GetRequiredUserId();
            var events = await _eventService.GetExpertEventsAsync(currentUserId);
            return Ok(events);
        }

        /// <summary>
        /// Xem chi tiết một sự kiện
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDetails(int id)
        {
            var ev = await _eventService.GetEventDetailsAsync(id);
            if (ev == null) return NotFound(new { message = "Không tìm thấy sự kiện." });
            return Ok(ev);
        }

        /// <summary>
        /// Chuyên gia chấm điểm cho bài thi trong sự kiện
        /// </summary>
        [HttpPost("submit-rating")]
        public async Task<IActionResult> SubmitRating([FromBody] ExpertRatingRequest request)
        {
            try
            {
                await _eventService.SubmitExpertRatingAsync(request);
                return Ok(new { message = "Chấm điểm thành công và đã cập nhật bảng điểm tổng." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Chốt sự kiện, công bố kết quả và tự động giải ngân tiền thưởng
        /// </summary>
        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinalizeEvent(int id)
        {
            try
            {
                await _eventService.FinalizeEventAndDistributePrizesAsync(id);
                return Ok(new { message = "Sự kiện đã kết thúc. Tiền thưởng đã được chuyển đến ví của những người thắng cuộc." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}