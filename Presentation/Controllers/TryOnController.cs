using Application.Request.TryOn;
using Application.Services.TryOn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TryOnController : ControllerBase
    {
        private readonly ITryOnService _tryOnService;

        public TryOnController(ITryOnService tryOnService)
        {
            _tryOnService = tryOnService;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetTryOnInfo()
        {
            var result = await _tryOnService.GetTryOnInfoAsync();

            return Ok(new
            {
                success = true,
                message = "Lấy thông tin thử đồ thành công.",
                data = result
            });
        }

        [HttpPost("try-on")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TryOn([FromForm] TryOnRequest request)
        {
            if (request.ModelImage == null || request.ClothImage == null)
                throw new ArgumentException("Thiếu ảnh input.");

            var networkStream = await _tryOnService.ProcessTryOnAsync(request.ModelImage, request.ClothImage, request.Category);
            var memoryStream = new MemoryStream();
            await networkStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return File(memoryStream, "image/png");
        }
    }
}