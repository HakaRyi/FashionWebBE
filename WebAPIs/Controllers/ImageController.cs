using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Services.Implements.ImageImp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;
        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }
        // GET: api/<ImageController>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _imageService.GetAllAvatar();
            if (result != null && result.Count > 0)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new { Message = "No images found" });
            }
        }

        // GET api/<ImageController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _imageService.GetByIdAsync(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new { Message = "Image not found" });
            }
        }

        // POST api/<ImageController>
        [HttpPost("create-avatar")]
        public async Task<IActionResult> Post([FromBody] Image image)
        {
            var userId = User.FindFirst("AccountID")?.Value;
            var result = await _imageService.CreateAvatarImage(int.Parse(userId),image);
            if(result > 0)
            {
                return Ok(new { Message = "Avatar image created successfully", ImageId = result });
            }
            else
            {
                return StatusCode(500, new { Message = "Failed to create avatar image" });
            }
        }


        // DELETE api/<ImageController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            var result = await _imageService.DeteleImage(id);
            if (result)
            {
                return Ok(new { Message = "Image deleted successfully" });
            }
            else
            {
                return StatusCode(500, new { Message = "Failed to delete image" });
            }
        }
    }
}
