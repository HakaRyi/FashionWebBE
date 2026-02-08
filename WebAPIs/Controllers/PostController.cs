using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.PostImp;
using Services.Request.PostReq;

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
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _postService.CreatePostAsync(request);

                // Trả về 202 Accepted (Đã nhận yêu cầu, đang xử lý) hoặc 201 Created
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
    }
}
