using Application.Request.UserReportReq;
using Application.Services.UserReportImp;
using Application.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class UserReportsController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public UserReportsController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetReportTypes()
        {
            var items = await _userReportService.GetReportTypesAsync();

            return Ok(new
            {
                message = "Lấy danh sách loại báo cáo thành công.",
                data = items
            });
        }

        [HttpPost("posts/{postId:int}")]
        public async Task<IActionResult> ReportPost(
            [FromRoute] int postId,
            [FromBody] CreateUserReportRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu báo cáo không hợp lệ."
                });
            }

            var userId = User.GetUserId();

            var dto = new CreateUserReportRequestDto
            {
                PostId = postId,
                ReportTypeId = request.ReportTypeId,
                Reason = request.Reason
            };

            var result = await _userReportService.CreateReportAsync(dto, userId);

            return Ok(new
            {
                message = result.Message,
                data = result
            });
        }
    }
}