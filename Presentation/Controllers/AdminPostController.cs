using Application.Services.AdminImp;
using Application.Services.PostImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Request.PostReq;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/post")]
    public class AdminPostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IDashboardService _dashboardService;

        public AdminPostController(IPostService postService, IDashboardService dashboardService)
        {
            _postService = postService;
            _dashboardService = dashboardService;
        }

        [HttpPut("{postId:int}/status")]
        public async Task<IActionResult> UpdatePostStatus(
            int postId,
            [FromBody] CheckPostRequest request)
        {
            var message = await _postService.AdminCheckTheStatusPost(request, postId);

            if (message == "Post not found.")
                return NotFound(new { Message = message });

            if (message == "Invalid status."
                || message == "Invalid status transition."
                || message == "Status is already set."
                || message == "Status is required.")
            {
                return BadRequest(new { Message = message });
            }

            return Ok(new
            {
                PostId = postId,
                Status = request.Status,
                Message = message
            });
        }
    }
}