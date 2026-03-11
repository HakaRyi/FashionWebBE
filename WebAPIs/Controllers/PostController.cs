using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PostImp;
using Services.Request.PostReq;
using Services.Utils;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.GetUserId();

                var result = await _postService.CreatePostAsync(userId, request);

                return CreatedAtAction(
                    nameof(GetPostById),
                    new { id = result.PostId },
                    result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdatePost(
            int id,
            [FromForm] UpdatePostRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.GetUserId();

                var result = await _postService.UpdatePostAsync(id, userId, request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            try
            {
                await _postService.DeletePostAsync(id);

                return Ok(new { message = "Post deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            var result = await _postService.GetAllPostAsync();

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPostById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            var result = await _postService.GetPostByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Post not found." });

            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyPosts()
        {
            var userId = User.GetUserId();

            var result = await _postService.GetAllMyPostAsync(userId);

            return Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetPostsByUser(
            int userId,
            [FromQuery] int pageSize = 10)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user id." });

            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var result = await _postService.GetPostsByUserAsync(userId, pageSize);

            return Ok(result);
        }

        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetFeed(
            [FromQuery] DateTime? cursor,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var result = await _postService.GetFeedAsync(userId, cursor, pageSize);

            return Ok(result);
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingPosts(
            [FromQuery] int limit = 10)
        {
            if (limit <= 0)
                limit = 10;

            if (limit > 50)
                limit = 50;

            var result = await _postService.GetTrendingPostsAsync(limit);

            return Ok(result);
        }

        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCheckTheStatusPost(
            int id,
            [FromBody] CheckPostRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _postService.AdminCheckTheStatusPost(request, id);

            if (result == "Post not found.")
                return NotFound(new { message = result });

            if (result.Contains("Invalid") || result.Contains("not"))
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }
    }
}