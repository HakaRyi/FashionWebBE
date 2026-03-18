using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.SocialImp;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class ReactionController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public ReactionController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        [HttpPost("post/{postId:int}/like")]
        public async Task<IActionResult> TogglePostLike(int postId)
        {
            var userId = User.GetUserId();

            var result = await _socialService
                .TogglePostReactionAsync(userId, postId);

            return Ok(result);
        }

        [HttpPost("comment/{commentId:int}/like")]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            var userId = User.GetUserId();

            var result = await _socialService
                .ToggleCommentReactionAsync(userId, commentId);

            return Ok(result);
        }
    }
}