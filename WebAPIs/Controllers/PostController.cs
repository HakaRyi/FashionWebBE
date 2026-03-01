using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PostImp;
using Services.Request.PostReq;
using System.Security.Claims;

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

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                return Unauthorized();

            try
            {
                var result = await _postService.CreatePostAsync(accountId, request);

                return CreatedAtAction(
                    nameof(GetPostsById),
                    new { id = result.PostId },
                    result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int userId))
                return Unauthorized();

            var result = await _postService.GetAllMyPostAsync(userId);
            return Ok(result);
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
    }
}