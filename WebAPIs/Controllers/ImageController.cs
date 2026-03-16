using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.ImageImp;
using Services.Request.ImageReq;
using Services.Utils;
using Services.Utils.Validator;
using System.Security.Claims;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ICloudStorageService _cloudService;

        public ImageController(IImageService imageService, ICloudStorageService cloudService)
        {
            _imageService = imageService;
            _cloudService = cloudService;
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid image id." });

            var image = await _imageService.GetByIdAsync(id);

            if (image == null)
                return NotFound(new { message = "Image not found." });

            return Ok(image);
        }


        [HttpPost("avatar")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAvatar([FromForm] UploadAvatarRequest request)
        {
            try
            {
                ImageUploadValidator.Validate(request.File);

                var userId = User.GetUserId();

                var imageUrl = await _cloudService.UploadImageAsync(request.File);

                var image = await _imageService.CreateAvatarImageAsync(userId, imageUrl);

                return Created("", image);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid image id." });

            try
            {
                await _imageService.DeleteImageAsync(id);

                return Ok(new { message = "Image deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Image not found." });
            }
        }

        [HttpGet("my-avatar")]
        [Authorize]
        public async Task<IActionResult> GetMyNewestAvatar()
        {
            try
            {
                var userId = User.GetUserId();

                var avatar = await _imageService.GetNewestAvatarAsync(userId);

                if (avatar == null)
                    return NotFound(new { message = "User has no avatar." });

                return Ok(avatar);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("my-avatars")]
        [Authorize]
        public async Task<IActionResult> GetMyAvatars()
        {
            try
            {
                var userId = User.GetUserId();

                var avatars = await _imageService.GetAllMyAvatarAsync(userId);

                return Ok(avatars);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}