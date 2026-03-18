using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Social.Comment;
using Services.Implements.SocialImp;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class CommentsController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public CommentsController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        [HttpGet("posts/{postId:int}/comments")]
        public async Task<IActionResult> GetComments(
            int postId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var userId = User.GetUserId();

            var response = await _socialService.GetCommentsAsync(userId, postId, skip, take);

            return Ok(response);
        }

        [HttpPost("posts/{postId:int}/comments")]
        public async Task<IActionResult> CreateComment(
            int postId,
            [FromBody] CreateCommentRequestDto request)
        {
            var userId = User.GetUserId();

            var comment = await _socialService.CreateCommentAsync(userId, postId, request);

            return Ok(comment);
        }

        [HttpGet("comments/{commentId:int}/replies")]
        public async Task<IActionResult> GetReplies(
            int commentId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var userId = User.GetUserId();

            var response = await _socialService.GetRepliesAsync(userId, commentId, skip, take);

            return Ok(response);
        }

        [HttpPost("comments/{commentId:int}/replies")]
        public async Task<IActionResult> ReplyComment(
            int commentId,
            [FromBody] CreateReplyDto request)
        {
            var userId = User.GetUserId();

            var response = await _socialService.ReplyCommentAsync(userId, commentId, request);

            return Ok(response);
        }

        [HttpPut("comments/{commentId:int}")]
        public async Task<IActionResult> UpdateComment(
            int commentId,
            [FromBody] CommentRequest request)
        {
            var userId = User.GetUserId();

            await _socialService.UpdateCommentAsync(commentId, userId, request);

            return NoContent();
        }

        [HttpDelete("comments/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.GetUserId();

            await _socialService.DeleteCommentAsync(commentId, userId);

            return NoContent();
        }
    }
}