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

            var resultStream = await _tryOnService.ProcessTryOnAsync(
                request.ModelImage,
                request.ClothImage,
                request.Category
            );

            resultStream.Position = 0;
            return File(resultStream, "image/png");

            //    var networkStream = await _tryOnService.ProcessTryOnAsync(request.ModelImage, request.ClothImage, request.Category);
            //    var memoryStream = new MemoryStream();
            //    await networkStream.CopyToAsync(memoryStream);
            //    memoryStream.Position = 0;
            //    return File(memoryStream, "image/png");
            //}
            //catch (Exception ex)
            //{
            //    // Log lỗi ra để xem
            //    Console.WriteLine(ex.ToString());
            //    return StatusCode(500, new { error = ex.Message });
            //}

        }
    }
}