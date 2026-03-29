using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PostImp;
using Services.Request.PostReq;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/post")]
    public class AdminPostController : ControllerBase
    {
        private readonly IPostService _postService;

        public AdminPostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpGet("pending-admin")]
        public async Task<IActionResult> GetPendingAdminPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _postService.GetPendingAdminPostsAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _postService.GetRejectedPostsAsync(page, pageSize);
            return Ok(result);
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