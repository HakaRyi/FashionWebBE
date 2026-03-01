using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.ImageImp;
using Services.Request.ImageReq;
using Services.Utils;
using System.Security.Claims;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ICloudStorageService _cloudService;

        public ImageController(
            IImageService imageService,
            ICloudStorageService cloudService)
        {
            _imageService = imageService;
            _cloudService = cloudService;
        }

        [HttpGet("avatars")]
        public async Task<IActionResult> GetAllAvatar()
        {
            var result = await _imageService.GetAllAvatarAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var image = await _imageService.GetByIdAsync(id);

            if (image == null)
                return NotFound(new { message = "Image not found" });

            return Ok(image);
        }

        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAvatar([FromForm] UploadAvatarRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is required");

            var userId = GetUserId();

            var imageUrl = await _cloudService.UploadImageAsync(request.File);
            var image = await _imageService.CreateAvatarImageAsync(userId, imageUrl);

            return Ok(new
            {
                message = "Avatar uploaded successfully",
                imageId = image.ImageId,
                url = image.ImageUrl
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _imageService.DeleteImageAsync(id);
            return Ok(new { message = "Image deleted successfully" });
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new Exception("User not authenticated");

            return int.Parse(claim.Value);
        }
    }
}