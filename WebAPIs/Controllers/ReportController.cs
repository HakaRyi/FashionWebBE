using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Dto.Social.Report;
using Application.Utils;
using Application.Services.Report;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api")]
    public class ReportController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public ReportController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        [HttpGet("report-types")]
        public async Task<IActionResult> GetReportTypes()
        {
            var result = await _userReportService.GetReportTypesAsync();
            return Ok(result);
        }

        [HttpPost("posts/{postId:int}/report")]
        [Authorize]
        public async Task<IActionResult> ReportPost(
            int postId,
            [FromBody] CreatePostReportDto request)
        {
            var accountId = User.GetUserId();

            var result = await _userReportService.ReportPostAsync(postId, accountId, request);
            return Ok(result);
        }
    }
}