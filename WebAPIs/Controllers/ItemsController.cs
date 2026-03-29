using Microsoft.AspNetCore.Mvc;
using Services.Implements.Items;
using Services.Response.ItemResp;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/item")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemsController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet("/api/items")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAll()
        {
            var results = await _itemService.GetAllItemsAsync();
            return Ok(results);
        }
        [HttpGet("/api/my-items")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetMyItems()
        {
            var accountId = int.Parse(User.FindFirst("AccountId")?.Value!);
            var results = await _itemService.GetMyItemsAsync(accountId);
            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemResponseDto>> GetById(int id)
        {
            var result = await _itemService.GetItemByIdAsync(id);

            if (result == null)
                return NotFound(new { Message = $"Không tìm thấy sản phẩm với ID {id}" });

            return Ok(result);
        }

        [HttpPost("upload")]
        //[Consumes("multipart/form-data")]
        public async Task<ActionResult<ItemResponseDto>> Create([FromBody] ProductUploadDto dto)
        {

            try
            {
                var accountId = int.Parse(User.FindFirst("AccountId")?.Value!);
                var result = await _itemService.CreateFashionItemAsync(dto, accountId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("recommend")]
        public async Task<ActionResult<List<ItemResponseDto>>> GetRecommendations([FromQuery] string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return BadRequest("Prompt cannot be empty");

            var results = await _itemService.GetRecommendationsAsync(prompt);
            return Ok(results);
        }
    }
}
