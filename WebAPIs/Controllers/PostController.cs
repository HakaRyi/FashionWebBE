using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        protected readonly IPostService _postService;
        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized();
                }

                var result = await _postService.CreatePostAsync(accountId, request);

                return StatusCode(201, new
                {
                    message = "Bài viết đang được xử lý.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet] //cho admin
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var result = await _postService.GetAllPostAsync();
                if (result == null || !result.Any())
                {
                    return NotFound(new { message = "Không có bài viết nào." });
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("detail/{id}")] //cho admin
        public async Task<IActionResult> GetPostsById(int id)
        {
            try
            {
                var result = await _postService.GetPostByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { message = "Không có bài viết nào cho tài khoản này." });
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPut("adminCheck/{id}")] //cho admin
        public async Task<IActionResult> AdminCheckTheStatusPost([FromBody] CheckPostRequest request, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _postService.AdminCheckTheStatusPost(request, id);
                if (result == "Post not found.")
                {
                    return NotFound(new { message = result });
                }
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
