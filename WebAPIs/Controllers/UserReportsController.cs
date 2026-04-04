using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Dto.Social.Report;
using Application.Utils;
using Application.Services.Report;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class UserReportsController : ControllerBase
    {
        private readonly IUserReportService _userReportService;

        public UserReportsController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        [HttpGet("types")]
        [Authorize]
        public async Task<IActionResult> GetReportTypes()
        {
            var items = await _userReportService.GetReportTypesAsync();
            return Ok(items);
        }

        [HttpPost("posts/{postId:int}")]
        [Authorize]
        public async Task<IActionResult> ReportPost(
            int postId,
            [FromBody] CreatePostReportDto request)
        {
            var userId = User.GetUserId();

            var result = await _userReportService.ReportPostAsync(
                postId,
                userId,
                request);

            return Ok(result);
        }
    }
}