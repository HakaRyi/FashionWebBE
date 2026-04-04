using Application.Services.PostImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Utils;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/post")]
    public class PostSaveController : ControllerBase
    {
        private readonly IPostSaveService _postSaveService;

        public PostSaveController(IPostSaveService postSaveService)
        {
            _postSaveService = postSaveService;
        }

        [HttpPost("{postId:int}/save")]
        public async Task<IActionResult> SavePost(int postId)
        {
            var userId = User.GetUserId();
            var created = await _postSaveService.SavePostAsync(postId, userId);

            return Ok(new
            {
                message = created ? "Saved post successfully." : "Post already saved.",
                data = new
                {
                    postId,
                    isSaved = true,
                    created
                }
            });
        }

        [HttpDelete("{postId:int}/save")]
        public async Task<IActionResult> UnsavePost(int postId)
        {
            var userId = User.GetUserId();
            var removed = await _postSaveService.UnsavePostAsync(postId, userId);

            return Ok(new
            {
                message = removed ? "Unsaved post successfully." : "Post was not saved.",
                data = new
                {
                    postId,
                    isSaved = false,
                    removed
                }
            });
        }

        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();
            var result = await _postSaveService.GetSavedPostsAsync(userId, page, pageSize);

            return Ok(new
            {
                message = "Get saved posts successfully.",
                data = result
            });
        }
    }
}