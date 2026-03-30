using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.EventAwardingImp;
using Services.Implements.EventCreationImp;
using Services.Implements.Events;
using Services.Request.EventReq;


namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/events")]
    //[Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IEventCreationService _eventCreationService;
        private readonly IEventAwardingService _eventAwardingService;

        public EventsController(IEventService eventService, IEventCreationService eventCreationService, IEventAwardingService eventAwardingService)
        {
            _eventService = eventService;
            _eventCreationService = eventCreationService;
            _eventAwardingService = eventAwardingService;
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
                var result = await _eventCreationService.CreateEventAsync(request);

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
        /// Chốt sự kiện, công bố kết quả và tự động giải ngân tiền thưởng
        /// </summary>
        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinalizeEvent(int id)
        {
            try
            {
                await _eventAwardingService.FinalizeAndAwardEventAsync(id);
                return Ok(new { message = "Sự kiện đã kết thúc. Tiền thưởng đã được chuyển đến ví của những người thắng cuộc." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        [HttpGet("expert-dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] string period = "30d")
        {
            var result = await _eventService.GetAnalyticsAsync(period);
            return Ok(result);
        }

        /// <summary>
        /// Kích hoạt sự kiện sớm hơn dự kiến (Chỉ dành cho Host)
        /// </summary>
        /// <param name="id">ID của sự kiện</param>
        [HttpPost("{id}/manual-start")]
        public async Task<IActionResult> ManualStart(int id)
        {
            try
            {
                await _eventCreationService.ManualStartEventAsync(id);

                return Ok(new
                {
                    Message = "Sự kiện đã được kích hoạt thành công sớm hơn dự kiến.",
                    ActivatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #region --- Queries (Lấy dữ liệu) ---

        /// <summary>
        /// Dành cho User: Xem các sự kiện công khai (Inviting, Active, Completed)
        /// </summary>
        [HttpGet("public")]
        //[AllowAnonymous]
        public async Task<IActionResult> GetAllForUser()
        {
            var result = await _eventService.GetAllEventsForUserAsync();
            return Ok(result);
        }

        /// <summary>
        /// Dành cho Expert: Xem sự kiện do mình tạo hoặc được mời chấm điểm
        /// </summary>
        [HttpGet("all-related-events-")]
        //[Authorize(Roles = "Expert")]
        public async Task<IActionResult> GetAllForExpert()
        {
            var result = await _eventService.GetAllEventsForExpertAsync();
            return Ok(result);
        }

        /// <summary>
        /// Dành cho Admin: Quản lý toàn bộ sự kiện trong hệ thống
        /// </summary>
        [HttpGet("all")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var result = await _eventService.GetAllEventsForAdminAsync();
            return Ok(result);
        }

        #endregion

        #region --- Management (Duyệt/Từ chối) ---

        /// <summary>
        /// Admin phê duyệt sự kiện để chuyển sang trạng thái mời Expert (Inviting)
        /// </summary>
        [HttpPost("{id}/approve")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            try
            {
                await _eventService.ApproveEventAsync(id);
                return Ok(new { message = "Sự kiện đã được phê duyệt thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin từ chối sự kiện và hoàn tiền cho người tạo
        /// </summary>
        [HttpPost("{id}/reject")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectEvent(int id, [FromBody] RejectEventRequest request)
        {
            try
            {
                await _eventService.RejectEventAsync(id, request.Reason);
                return Ok(new { message = "Đã từ chối sự kiện và hoàn trả lại tiền cho chủ sự kiện." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region --- Dashboard & Analytics ---

        /// <summary>
        /// Lấy dữ liệu phân tích Dashboard (Dành cho Expert/Admin)
        /// </summary>
        /// <param name="period">7days, 30days, 90days</param>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string period = "30days")
        {
            var result = await _eventService.GetAnalyticsAsync(period);
            return Ok(result);
        }

        #endregion

        #region --- General Queries (User & Guest) ---

        /// <summary>
        /// Lấy chi tiết một sự kiện cụ thể
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventDetails(int id)
        {
            var detail = await _eventService.GetEventDetailsAsync(id);
            if (detail == null) return NotFound(new { message = "Không tìm thấy sự kiện." });
            return Ok(detail);
        }

        #endregion

        #region --- Expert Specific ---

        /// <summary>
        /// Lấy danh sách các sự kiện liên quan đến Expert (Tạo hoặc được mời)
        /// </summary>
        [HttpGet("expert/all")]
        public async Task<IActionResult> GetExpertRelatedEvents()
        {
            var events = await _eventService.GetAllEventsForExpertAsync();
            return Ok(events);
        }

        /// <summary>
        /// Lấy danh sách sự kiện do chính Expert này tạo ra
        /// </summary>
        [HttpGet("expert/my-created")]
        public async Task<IActionResult> GetMyCreatedEvents()
        {
            var events = await _eventService.GetMyCreatedEventsAsync();
            return Ok(events);
        }

        #endregion

        #region --- Admin Specific ---

        /// <summary>
        /// Lấy TẤT CẢ sự kiện trong hệ thống (Chỉ dành cho Admin)
        /// </summary>
        [HttpGet("admin/all")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllEventsForAdmin()
        {
            var events = await _eventService.GetAllEventsForAdminAsync();
            return Ok(events);
        }

        #endregion


        [HttpGet("{id}/leaderboard")]
        public async Task<IActionResult> GetLeaderboard(int id)
        {
            var result = await _eventService.GetEventLeaderboardAsync(id);
            return Ok(result);
        }


        [HttpGet("{id}/my-result")]
        [Authorize]
        public async Task<IActionResult> GetMyResult(int id)
        {


            var result = await _eventService.GetMyResultDetailAsync(id);
            if (result == null) return NotFound(new { message = "Bạn chưa tham gia hoặc chưa có điểm trong sự kiện này." });

            return Ok(result);
        }

        //[HttpGet("{id}/posts")]
        //public async Task<IActionResult> GetEventPosts(int id)
        //{
        //    // Tận dụng hàm đã có của PostRepo
        //    var posts = await _postRepo.GetPostsByEventIdAsync(id);
        //    // Chuyển đổi sang PostResponse hoặc Dto tùy ý
        //    return Ok(posts);
        //}
    }
}

public class RejectEventRequest
{
    public string Reason { get; set; } = null!;
}
