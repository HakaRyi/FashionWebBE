using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Dto.Wardrobe;
using Services.Implements.Auth;
using Services.Implements.Items;
using Services.Request.ItemReq;
using Services.Request.ItemRequest;
using Services.Response.ItemResp;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/items")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ICurrentUserService _currentUserService;

        public ItemsController(
            IItemService itemService,
            ICurrentUserService currentUserService)
        {
            _itemService = itemService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAll()
        {
            var results = await _itemService.GetAllItemsAsync();

            return Ok(new
            {
                message = "Lấy danh sách item thành công.",
                data = results
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetMyItems()
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var results = await _itemService.GetMyItemsAsync(accountId);

            return Ok(new
            {
                message = "Lấy danh sách item của tôi thành công.",
                data = results
            });
        }

        // Route này chỉ nên dùng cho internal / owner / admin nếu bạn muốn giữ.
        // Không nên cho FE public detail dùng route này.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemResponseDto>> GetById(int id)
        {
            var result = await _itemService.GetItemByIdAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy item với ID = {id}."
                });
            }

            return Ok(new
            {
                message = "Lấy chi tiết item thành công.",
                data = result
            });
        }

        [HttpGet("public/{itemId:int}")]
        public async Task<ActionResult<PublicWardrobeItemDetailDto>> GetPublicItemDetail(int itemId)
        {
            var result = await _itemService.GetPublicItemDetailAsync(itemId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy món đồ công khai."
                });
            }

            return Ok(new
            {
                message = "Lấy chi tiết món đồ công khai thành công.",
                data = result
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ItemResponseDto>> Create([FromBody] ProductUploadDto dto)
        {
            try
            {
                var accountId = _currentUserService.GetRequiredUserId();
                var result = await _itemService.CreateFashionItemAsync(dto, accountId);

                return Ok(new
                {
                    message = "Tạo item thành công.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi hệ thống khi tạo item.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("smart-match")]
        public async Task<ActionResult<List<ItemResponseDto>>> GetSmartRecommendations([FromBody] SmartRecommendationRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Request không hợp lệ."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Prompt) &&
                (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return BadRequest(new
                {
                    message = "Vui lòng nhập prompt hoặc chọn ReferenceItemId để AI gợi ý."
                });
            }

            try
            {
                var recommendations = await _itemService.GetSmartRecommendationsAsync(request);

                if (recommendations == null || recommendations.Count == 0)
                {
                    return Ok(new
                    {
                        message = "Không tìm thấy món đồ phù hợp với yêu cầu của bạn.",
                        data = new List<ItemResponseDto>()
                    });
                }

                return Ok(new
                {
                    message = "Lấy danh sách gợi ý thành công.",
                    data = recommendations
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartMatch Error] {ex.Message}");

                return StatusCode(500, new
                {
                    message = "Hệ thống AI đang gặp sự cố, vui lòng thử lại sau.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPut("{itemId:int}")]
        public async Task<ActionResult> Update([FromRoute] int itemId, [FromBody] UpdateItemRequest request)
        {
            try
            {
                await _itemService.UpdateItemAsync(itemId, request);

                return Ok(new
                {
                    message = "Cập nhật item thành công."
                });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi hệ thống khi cập nhật item.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpDelete("{itemId:int}")]
        public async Task<ActionResult> Delete([FromRoute] int itemId)
        {
            try
            {
                await _itemService.DeleteItemAsync(itemId);

                return Ok(new
                {
                    message = "Xóa item thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi hệ thống khi xóa item.",
                    error = ex.Message
                });
            }
        }
    }
}