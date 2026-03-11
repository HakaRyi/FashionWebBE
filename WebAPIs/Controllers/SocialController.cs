using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.SocialImp;
using Services.Request.CommentReq;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class SocialController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public SocialController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        // ================= LIKE =================

        [HttpPost("posts/{postId}/like")]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var userId = User.GetUserId();

            var isLiked = await _socialService.ToggleLikeAsync(userId, postId);
            var likeCount = await _socialService.GetLikeCountAsync(postId);

            return Ok(new
            {
                isLiked,
                likeCount
            });
        }

        [HttpGet("posts/{postId}/likes/count")]
        public async Task<IActionResult> GetLikeCount(int postId)
        {
            var count = await _socialService.GetLikeCountAsync(postId);

            return Ok(new { likeCount = count });
        }

        // ================= COMMENT =================

        [HttpGet("posts/{postId}/comments")]
        public async Task<IActionResult> GetComments(int postId)
        {
            var comments = await _socialService.GetCommentsByPostIdAsync(postId);

            return Ok(comments);
        }

        [HttpGet("posts/{postId}/comments/count")]
        public async Task<IActionResult> GetCommentCount(int postId)
        {
            var count = await _socialService.GetCommentCountAsync(postId);

            return Ok(new { commentCount = count });
        }

        [HttpPost("posts/{postId}/comments")]
        public async Task<IActionResult> CreateComment(
            int postId,
            [FromBody] CommentRequest request)
        {
            var userId = User.GetUserId();

            var commentId = await _socialService
                .CreateCommentAsync(request, userId, postId);

            return Ok(new
            {
                message = "Comment created successfully.",
                commentId
            });
        }

        [HttpPut("comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(
            int commentId,
            [FromBody] CommentRequest request)
        {
            var userId = User.GetUserId();

            await _socialService
                .UpdateCommentAsync(commentId, userId, request);

            return Ok(new
            {
                message = "Comment updated successfully."
            });
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.GetUserId();

            await _socialService
                .DeleteCommentAsync(commentId, userId);

            return Ok(new
            {
                message = "Comment deleted successfully."
            });
        }
    }
}