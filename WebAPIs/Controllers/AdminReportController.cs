using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Social.Report;
using Services.Implements.Report;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public AdminReportController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

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

            return Ok(result);
        }

        [HttpGet("{userReportId:int}")]
        public async Task<IActionResult> GetReportDetail(int userReportId)
        {
            var result = await _userReportService.GetAdminReportDetailAsync(userReportId);
            return Ok(result);
        }

        [HttpPatch("{userReportId:int}/status")]
        public async Task<IActionResult> UpdateReportStatus(
            int userReportId,
            [FromBody] UpdateReportStatusDto request)
        {
            var adminId = User.GetUserId();

            var result = await _userReportService.UpdateReportStatusAsync(
                userReportId,
                adminId,
                request);

            return Ok(result);
        }
    }
}