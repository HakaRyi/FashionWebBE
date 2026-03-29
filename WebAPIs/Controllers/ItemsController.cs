using Microsoft.AspNetCore.Mvc;
using Services.Implements.Items;
using Services.Request.ItemReq;

using Services.Request.ItemRequest;
using Services.Request.ItemReq;
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

        //[HttpGet("recommendv1")]
        //public async Task<ActionResult<List<ItemResponseDto>>> GetRecommendations([FromQuery] string prompt)
        //{
        //    if (string.IsNullOrWhiteSpace(prompt))
        //        return BadRequest("Prompt cannot be empty");

        //    var results = await _itemService.GetRecommendationsAsync(prompt);
        //    return Ok(results);
        //}

        /// <summary>
        /// Gợi ý phối đồ thông minh bằng AI (Gemini + Vector Search)
        /// Hỗ trợ cả 2 luồng: Prompt tự do và Tìm đồ phối cùng 1 Item cụ thể.
        /// </summary>
        [HttpPost("smart-match")]
        public async Task<ActionResult<List<ItemResponseDto>>> GetSmartRecommendations([FromBody] SmartRecommendationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt) && (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return BadRequest(new
                {
                    Message = "Vui lòng nhập câu lệnh (Prompt) hoặc chọn một món đồ (ReferenceItemId) để AI có thể gợi ý."
                });
            }

            try
            {
                var recommendations = await _itemService.GetSmartRecommendationsAsync(request);

                // Nếu không tìm thấy kết quả nào phù hợp
                if (recommendations == null || recommendations.Count == 0)
                {
                    return Ok(new
                    {
                        Message = "Rất tiếc, không tìm thấy món đồ nào phù hợp với yêu cầu của bạn lúc này.",
                        Data = new List<ItemResponseDto>()
                    });
                }

                return Ok(new
                {
                    Message = "Lấy danh sách gợi ý thành công!",
                    Data = recommendations
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartMatch Error]: {ex.Message}");
                return StatusCode(500, new
                {
                    Message = "Hệ thống AI đang gặp chút sự cố, vui lòng thử lại sau.",
                    Error = ex.Message
                });
            }
        }
        [HttpPut("update/{itemId}")]
        public async Task<ActionResult> Update([FromRoute]int itemId,[FromBody] UpdateItemRequest request)
        {
            await _itemService.UpdateItem(itemId,request);
            return Ok(new
            {
                message = "cap nhat thanh cong"
            });
        }
        [HttpDelete("delete/{itemId}")]
        public async Task<ActionResult> Delete([FromRoute] int itemId)
        {
            await _itemService.DeleteItem(itemId);
            return Ok(new
            {
                message = "xoa thanh cong"
            });
        }

    }
}
