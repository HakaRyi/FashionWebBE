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
                    nameof(GetPostsById),
                    new { id = result.PostId },
                    result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostRequest request)
        {
            try
            {
                var accountId = int.Parse(User.FindFirst("AccountId")?.Value!);

                var result = await _postService.UpdatePostAsync(id, accountId, request);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            var result = await _postService.GetAllPostAsync();
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetAllMyPosts()
        {
            try
            {
                var userId = User.GetUserId();

                var result = await _postService.GetAllMyPostAsync(userId);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPostsById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            var result = await _postService.GetPostByIdAsync(id);

            if (result == null)
                return NotFound(new { message = "Post not found." });

            return Ok(result);
        }

        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCheckTheStatusPost(
            [FromBody] CheckPostRequest request,
            int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id <= 0)
                return BadRequest(new { message = "Invalid post id." });

            var result = await _postService.AdminCheckTheStatusPost(request, id);

            if (result == "Post not found.")
                return NotFound(new { message = result });

            if (result.Contains("Invalid") || result.Contains("not"))
                return BadRequest(new { message = result });

            return Ok(new { message = result });
        }

        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetFeed(
            [FromQuery] DateTime? cursor,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();

                var result = await _postService.GetFeedAsync(userId, cursor, pageSize);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}