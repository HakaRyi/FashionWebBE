using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Social.Post;
using Services.Implements.PostImp;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/post")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
        {
            var userId = User.GetUserId();

            var createdPost = await _postService.CreatePostAsync(userId, request);

            return Created($"/api/post/{createdPost.PostId}", createdPost);
        }

        [HttpPut("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(
            int postId,
            [FromBody] UpdatePostDto request)
        {
            var userId = User.GetUserId();

            await _postService.UpdatePostAsync(postId, userId, request);

            return NoContent();
        }

        [HttpDelete("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = User.GetUserId();

            await _postService.DeletePostAsync(postId, userId);

            return NoContent();
        }

        [HttpPatch("{postId:int}/hide")]
        [Authorize]
        public async Task<IActionResult> HidePost(int postId)
        {
            var userId = User.GetUserId();

            var response = await _postService.HidePostAsync(postId, userId);

            return Ok(response);
        }

        [HttpPatch("{postId:int}/unhide")]
        [Authorize]
        public async Task<IActionResult> UnhidePost(int postId)
        {
            var userId = User.GetUserId();

            var response = await _postService.UnhidePostAsync(postId, userId);

            return Ok(response);
        }

        [HttpGet("{postId:int}")]
        [Authorize]
        public async Task<IActionResult> GetPostDetail(int postId)
        {
            var userId = User.GetUserId();

            var post = await _postService.GetPostDetailAsync(postId, userId);

            if (post == null)
                return NotFound();

            return Ok(post);
        }

        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetFeed(
            [FromQuery] DateTime? cursor,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetFeedAsync(userId, cursor, pageSize);

            return Ok(posts);
        }

        [HttpGet("trending")]
        [Authorize]
        public async Task<IActionResult> GetTrending([FromQuery] int limit = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetTrendingPostsAsync(userId, limit);

            return Ok(posts);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            var posts = await _postService.GetMyPostsAsync(userId, page, pageSize);

            return Ok(posts);
        }

        [HttpGet("user/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserPosts(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            int? viewerId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                viewerId = User.GetUserId();
            }

            var posts = await _postService.GetUserPostsAsync(userId, viewerId, page, pageSize);

            return Ok(posts);
        }
    }
}