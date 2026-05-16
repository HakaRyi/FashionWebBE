using Application.Interfaces;
using Application.Request.UserReportReq;
using Application.Services.UserReportImp;
using Application.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public AdminReportsController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        /// <summary>
        /// Admin lấy danh sách report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] string? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _userReportService.GetAdminReportsAsync(
                status,
                pageNumber,
                pageSize);

            return Ok(new
            {
                message = "Lấy danh sách báo cáo thành công.",
                data = result
            });
        }

        /// <summary>
        /// Admin lấy chi tiết một report
        /// </summary>
        [HttpGet("{userReportId:int}")]
        public async Task<IActionResult> GetReportDetail([FromRoute] int userReportId)
        {
            var result = await _userReportService.GetAdminReportDetailAsync(userReportId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy báo cáo."
                });
            }

            return Ok(new
            {
                message = "Lấy chi tiết báo cáo thành công.",
                data = result
            });
        }

        /// <summary>
        /// Admin review report
        /// </summary>
        [HttpPut("{userReportId:int}/review")]
        public async Task<IActionResult> ReviewReport(
            [FromRoute] int userReportId,
            [FromBody] ReviewUserReportRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Dữ liệu review không hợp lệ."
                });
            }

            var adminId = User.GetUserId();

            var result = await _userReportService.ReviewReportAsync(
                userReportId,
                request,
                adminId);

            return Ok(new
            {
                message = result.Message,
                data = result
            });
        }
    }
}