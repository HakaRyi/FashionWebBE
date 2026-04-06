using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Domain.Dto.Social.Comment;
using Application.Utils;
using Application.Services.SocialImp;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class CommentController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public CommentController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        [HttpGet("post/{postId:int}/comment")]
        public async Task<IActionResult> GetComments(
            int postId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var userId = User.GetUserId();

            var response = await _socialService.GetCommentsAsync(userId, postId, skip, take);

            return Ok(response);
        }

        [HttpPost("post/{postId:int}/comment")]
        public async Task<IActionResult> CreateComment(
            int postId,
            [FromBody] CreateCommentRequestDto request)
        {
            var userId = User.GetUserId();

            var comment = await _socialService.CreateCommentAsync(userId, postId, request);

            return Ok(comment);
        }

        [HttpGet("comment/{commentId:int}/replies")]
        public async Task<IActionResult> GetReplies(
            int commentId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            var userId = User.GetUserId();

            var response = await _socialService.GetRepliesAsync(userId, commentId, skip, take);

            return Ok(response);
        }

        [HttpPost("comment/{commentId:int}/replies")]
        public async Task<IActionResult> ReplyComment(
            int commentId,
            [FromBody] CreateReplyDto request)
        {
            var userId = User.GetUserId();

            var response = await _socialService.ReplyCommentAsync(userId, commentId, request);

            return Ok(response);
        }

        [HttpPut("comment/{commentId:int}")]
        public async Task<IActionResult> UpdateComment(
            int commentId,
            [FromBody] CommentRequest request)
        {
            var userId = User.GetUserId();

            await _socialService.UpdateCommentAsync(commentId, userId, request);

            return NoContent();
        }

        [HttpDelete("comment/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.GetUserId();

            await _socialService.DeleteCommentAsync(commentId, userId);

            return NoContent();
        }
    }
}