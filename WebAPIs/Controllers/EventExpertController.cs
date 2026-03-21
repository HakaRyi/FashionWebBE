using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.EventExpertSer;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/event-expert")]
    //[Authorize(Roles = "Expert")]
    public class EventExpertController : ControllerBase
    {
        private readonly IEventExpertService _eventExpertService;

        public EventExpertController(IEventExpertService eventExpertService)
        {
            _eventExpertService = eventExpertService;
        }

        /// <summary>
        /// Chủ Event mời danh sách các Expert khác tham gia Hội đồng chấm điểm.
        /// </summary>
        /// <param name="eventId">ID của sự kiện</param>
        /// <param name="expertIds">Danh sách ID tài khoản của các Expert được mời</param>
        [HttpPost("invite/{eventId}")]
        public async Task<IActionResult> InviteExperts(int eventId, [FromBody] List<int> expertIds)
        {
            try
            {
                if (expertIds == null || expertIds.Count == 0)
                    return BadRequest("Danh sách Expert mời không được để trống.");

                var result = await _eventExpertService.InviteExpertsAsync(eventId, expertIds);

                if (result)
                    return Ok(new { message = "Gửi lời mời thành công." });

                return BadRequest(new { message = "Không có lời mời mới nào được gửi (có thể họ đã được mời trước đó)." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Expert phản hồi lời mời (Chấp nhận hoặc Từ chối).
        /// </summary>
        /// <param name="eventId">ID của sự kiện được mời</param>
        /// <param name="accept">true nếu đồng ý, false nếu từ chối</param>
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

        /// <summary>
        /// Lấy danh sách các sự kiện mà Expert hiện tại đang có lời mời chờ (Pending).
        /// </summary>
        [HttpGet("my-invitations")]
        public async Task<IActionResult> GetMyInvitations()
        {
            try
            {
                var invitations = await _eventExpertService.GetMyPendingInvitationsAsync();
                return Ok(invitations);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
