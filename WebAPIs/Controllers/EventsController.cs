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
    [Authorize] // Mặc định yêu cầu đăng nhập cho toàn bộ Controller
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
        /// Tạo sự kiện mới (Trạng thái: Pending_Payment). 
        /// Hệ thống sẽ tự động kích hoạt và ký quỹ vào StartTime nếu đủ điều kiện Expert.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateEvent([FromForm] CreateEventRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Gọi hàm tạo sự kiện mới (Đã bao gồm logic lập lịch Quartz bên trong Service)
                var result = await _eventService.CreateEventAsync(request);

                return Ok(new
                {
                    message = "Sự kiện đã được tạo thành công! Hệ thống đang chờ các Expert chấp nhận lời mời để kích hoạt.",
                    eventId = result.EventId,
                    status = result.Status,
                    startTime = result.StartTime
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Expert chủ động kích hoạt sự kiện sớm (Publish thủ công).
        /// Chỉ thành công nếu số lượng Expert đã Accepted >= MinExpertsRequired.
        /// </summary>
        [HttpPost("{id}/publish-now")]
        public async Task<IActionResult> PublishManual(int id)
        {
            try
            {
                // Gọi hàm kích hoạt và ký quỹ ngay lập tức
                await _eventService.ActivateEventWithEscrowAsync(id);
                return Ok(new { message = "Sự kiện đã được kích hoạt và ký quỹ thành công!" });
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

        /// <summary>
        /// Lấy danh sách các lời mời chấm điểm sự kiện cho Expert hiện tại
        /// </summary>
        [HttpGet("expert/invitations")]
        public async Task<IActionResult> GetInvitations()
        {
            var events = await _eventService.GetEventsInvitedToRateAsync();
            return Ok(events);
        }

        /// <summary>
        /// Danh sách sự kiện công khai cho người dùng tham gia
        /// </summary>
        [HttpGet("public/all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllForPublic()
        {
            var events = await _eventService.GetAllEventsForUserAsync();
            return Ok(events);
        }

        /// <summary>
        /// Lấy các bài post tham gia trong một sự kiện
        /// </summary>
        [HttpGet("{id}/posts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventPosts(int id)
        {
            var posts = await _eventService.GetPostsByEventIdAsync(id);
            return Ok(posts);
        }

        [HttpGet("expert-dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] string period = "30d")
        {
            var result = await _eventService.GetAnalyticsAsync(period);
            return Ok(result);
        }
    }
}