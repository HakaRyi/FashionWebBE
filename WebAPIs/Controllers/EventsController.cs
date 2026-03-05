using Microsoft.AspNetCore.Mvc;
using Services.Implements.Events;
using Services.Response.EventResp;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/event")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            // LƯU Ý: Ở thực tế, accountId từ User.Claims (Token JWT)
            int accountId = 3;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _eventService.CreateEventAsync(accountId, dto);

            if (result)
            {
                return Ok(new { message = "Khai mạc sự kiện thành công!", status = "Active" });
            }

            return BadRequest(new { message = "Tạo sự kiện thất bại. Vui lòng kiểm tra lại số dư hoặc thông tin." });
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositDto dto)
        {
            var result = await _eventService.DepositCoinsAsync(dto);

            if (result)
            {
                return Ok(new { message = "Giao dịch thành công!" });
            }

            return BadRequest(new { message = "Giao dịch thất bại." });
        }

        [HttpPost("calculate-score")]
        public async Task<IActionResult> CalculateScore(int postId, double expertGrade, double communityGrade, double weight)
        {
            try
            {
                await _eventService.CalculateFinalScoreAsync(postId, expertGrade, communityGrade, weight);
                return Ok(new { message = "Cập nhật điểm số thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
