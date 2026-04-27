using Application.Interfaces;
using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Application.Services.Items;
using Domain.Contracts.Wardrobe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
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
                message = "Get all items successfully.",
                data = results
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult> GetMyItems(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var accountId = _currentUserService.GetRequiredUserId();

            var result = await _itemService.GetMyItemsAsync(
                accountId,
                page,
                pageSize,
                search);

            return Ok(new
            {
                message = "Get my items successfully.",
                data = new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
                }
            });
        }

        [Authorize]
        [HttpGet("my-item")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAllMyItems()
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var results = await _itemService.GetAllMyItemsAsync(accountId);

            return Ok(new
            {
                message = "Get all my items successfully.",
                data = results
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemResponseDto>> GetById(int id)
        {
            var result = await _itemService.GetItemByIdAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    message = $"Item with ID = {id} was not found."
                });
            }

            return Ok(new
            {
                message = "Get item detail successfully.",
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
                    message = "Public item was not found."
                });
            }

            return Ok(new
            {
                message = "Get public item detail successfully.",
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
                    message = "Create item successfully.",
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
                    message = "System error while creating item.",
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
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Prompt) &&
                (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return BadRequest(new
                {
                    message = "Please provide prompt or ReferenceItemId."
                });
            }

            try
            {
                var recommendations = await _itemService.GetSmartRecommendationsAsync(request);

                if (recommendations == null || recommendations.Count == 0)
                {
                    return Ok(new
                    {
                        message = "No suitable items found.",
                        data = new List<ItemResponseDto>()
                    });
                }

                return Ok(new
                {
                    message = "Get smart recommendations successfully.",
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
                return StatusCode(500, new
                {
                    message = "AI recommendation failed. Please try again later.",
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
                    message = "Update item successfully."
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
                    message = "System error while updating item.",
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
                    message = "Delete item successfully."
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while deleting item.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // COMMERCE: publish / unpublish
        // =========================================================

        [Authorize]
        [HttpPost("{itemId:int}/publish")]
        public async Task<ActionResult<ItemCommerceResponseDto>> PublishItemForSale(
            [FromRoute] int itemId,
            [FromBody] PublishItemForSaleRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Invalid publish request."
                });
            }

            try
            {
                var result = await _itemService.PublishItemForSaleAsync(itemId, request);

                return Ok(new
                {
                    message = "Publish item for sale successfully.",
                    data = result
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while publishing item.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{itemId:int}/unpublish")]
        public async Task<ActionResult<ItemCommerceResponseDto>> UnpublishItem([FromRoute] int itemId)
        {
            try
            {
                var result = await _itemService.UnpublishItemAsync(itemId);

                return Ok(new
                {
                    message = "Unpublish item successfully.",
                    data = result
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while unpublishing item.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // VARIANTS
        // =========================================================

        [Authorize]
        [HttpGet("{itemId:int}/variants")]
        public async Task<ActionResult<List<ItemVariantResponseDto>>> GetItemVariants([FromRoute] int itemId)
        {
            try
            {
                var result = await _itemService.GetItemVariantsAsync(itemId);

                return Ok(new
                {
                    message = "Get item variants successfully.",
                    data = result
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
                    message = "System error while getting item variants.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{itemId:int}/variants")]
        public async Task<ActionResult<ItemVariantResponseDto>> CreateItemVariant(
            [FromRoute] int itemId,
            [FromBody] CreateItemVariantRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Invalid variant request."
                });
            }

            try
            {
                var result = await _itemService.CreateItemVariantAsync(itemId, request);

                return Ok(new
                {
                    message = "Create item variant successfully.",
                    data = result
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while creating item variant.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPut("variants/{itemVariantId:int}")]
        public async Task<ActionResult<ItemVariantResponseDto>> UpdateItemVariant(
            [FromRoute] int itemVariantId,
            [FromBody] UpdateItemVariantRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    message = "Invalid update variant request."
                });
            }

            try
            {
                var result = await _itemService.UpdateItemVariantAsync(itemVariantId, request);

                return Ok(new
                {
                    message = "Update item variant successfully.",
                    data = result
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while updating item variant.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpDelete("variants/{itemVariantId:int}")]
        public async Task<ActionResult> DeleteItemVariant([FromRoute] int itemVariantId)
        {
            try
            {
                await _itemService.DeleteItemVariantAsync(itemVariantId);

                return Ok(new
                {
                    message = "Delete item variant successfully."
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while deleting item variant.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("saved")]
        public async Task<ActionResult<List<PublicWardrobeItemDto>>> GetMySavedItems()
        {
            try
            {
                var result = await _itemService.GetMySavedItemsAsync();

                return Ok(new
                {
                    message = "Get saved items successfully.",
                    data = result
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
                    message = "System error while getting saved items.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{itemId:int}/save")]
        public async Task<ActionResult> SaveItem([FromRoute] int itemId)
        {
            try
            {
                await _itemService.SaveItemAsync(itemId);

                return Ok(new
                {
                    message = "Save item successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while saving item.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpDelete("{itemId:int}/save")]
        public async Task<ActionResult> UnsaveItem([FromRoute] int itemId)
        {
            try
            {
                await _itemService.UnsaveItemAsync(itemId);

                return Ok(new
                {
                    message = "Unsave item successfully."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "System error while unsaving item.",
                    error = ex.Message
                });
            }
        }
    }
}