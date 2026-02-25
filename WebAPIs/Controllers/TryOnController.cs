using Microsoft.AspNetCore.Mvc;
using Services.Implements.TryOn;
using Services.Request.TryOn;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TryOnController : ControllerBase
    {
        private readonly ITryOnService _tryOnService;

        public TryOnController(ITryOnService tryOnService)
        {
            _tryOnService = tryOnService;
        }

        [HttpPost("try-on")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TryOn([FromForm] TryOnRequest request)
        {
            try
            {
                if (request.ModelImage == null || request.ClothImage == null)
                    return BadRequest("Thiếu ảnh input");

                var networkStream = await _tryOnService.ProcessTryOnAsync(request.ModelImage, request.ClothImage);
                var memoryStream = new MemoryStream();
                await networkStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return File(memoryStream, "image/png");
            }
            catch (Exception ex)
            {
                // Log lỗi ra để xem
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
