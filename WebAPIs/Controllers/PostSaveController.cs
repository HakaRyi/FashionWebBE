using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PostImp;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/posts")]
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

            await _postSaveService.SavePostAsync(postId, userId);

            return Ok(new
            {
                postId,
                isSaved = true,
                message = "Saved post successfully"
            });
        }

        [HttpDelete("{postId:int}/save")]
        public async Task<IActionResult> UnsavePost(int postId)
        {
            var userId = User.GetUserId();

            await _postSaveService.UnsavePostAsync(postId, userId);

            return Ok(new
            {
                postId,
                isSaved = false,
                message = "Unsaved post successfully"
            });
        }

        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postSaveService.GetSavedPostsAsync(
                userId,
                page,
                pageSize);

            return Ok(posts);
        }
    }
}