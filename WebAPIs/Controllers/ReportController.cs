using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Social.Report;
using Services.Implements.Report;
using Services.Utils;

namespace WebAPIs.Controllers
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