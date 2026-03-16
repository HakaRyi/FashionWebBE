using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.SocialImp;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class ReactionsController : ControllerBase
    {
        private readonly ISocialService _socialService;

        public ReactionsController(ISocialService socialService)
        {
            _socialService = socialService;
        }

        [HttpPost("posts/{postId:int}/like")]
        public async Task<IActionResult> TogglePostLike(int postId)
        {
            var userId = User.GetUserId();

            var result = await _socialService
                .TogglePostReactionAsync(userId, postId);

            return Ok(result);
        }

        [HttpPost("comments/{commentId:int}/like")]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            var userId = User.GetUserId();

            var result = await _socialService
                .ToggleCommentReactionAsync(userId, commentId);

            return Ok(result);
        }
    }
}