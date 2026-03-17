using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Admin;
using Repositories.Dto.Social.Post;
using Services.Implements.PostImp;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/posts")]
    public class AdminPostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public AdminPostsController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpGet("ai-rejected")]
        public async Task<IActionResult> GetAIRejectedPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _postService.GetAIRejectedPostsAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("{postId:int}/review-ai-rejected")]
        public async Task<IActionResult> ReviewAIRejectedPost(
            int postId,
            [FromBody] AdminReviewAIRejectedRequestDto request)
        {
            await _postService.ReviewAIRejectedPostAsync(postId, request.Approve);

            return Ok(new
            {
                PostId = postId,
                Status = request.Approve ? "Published" : "BlockedByAdmin",
                Message = request.Approve
                    ? "Post approved and published successfully."
                    : "Post blocked by admin successfully."
            });
        }
    }
}