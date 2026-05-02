using Application.Interfaces;
using Application.Request.CollectionDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/collections")]
    public class CollectionController : ControllerBase
    {
        private readonly ICollectionService _collectionService;

        public CollectionController(ICollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCollection([FromBody] CollectionCreateDto dto)
        {
            await _collectionService.SaveCollectionAsync(dto);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCollections()
        {
            var result = await _collectionService.GetUserCollectionsAsync();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollection(int id)
        {
            var success = await _collectionService.DeleteCollectionAsync(id);
            if (!success) return NotFound("Không tìm thấy bộ sưu tập hoặc bạn không có quyền xóa.");
            return NoContent();
        }
        [HttpPut("{id}/items")]
        public async Task<IActionResult> UpdateCollectionItems(int id, [FromBody] CollectionUpdateDto dto)
        {
            if (dto.NewItemIds == null || !dto.NewItemIds.Any())
                return BadRequest("Danh sách món đồ không được để trống.");

            var success = await _collectionService.UpdateCollectionItemsAsync(id, dto);

            if (!success)
                return NotFound("Không tìm thấy bộ sưu tập hoặc có lỗi xảy ra trong quá trình cập nhật.");

            return Ok(new { message = "Cập nhật danh sách món đồ thành công." });
        }
        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddItemsToCollection(int id, [FromBody] List<int> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
                return BadRequest("Danh sách ID món đồ cần thêm không được để trống.");

            var success = await _collectionService.AddItemsToCollectionAsync(id, itemIds);

            if (!success)
                return NotFound("Không tìm thấy bộ sưu tập hoặc có lỗi xảy ra.");

            return Ok(new { message = "Đã thêm món đồ vào bộ sưu tập." });
        }

        [HttpDelete("{id}/items")]
        public async Task<IActionResult> RemoveItemsFromCollection(int id, [FromBody] List<int> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
                return BadRequest("Danh sách ID món đồ cần xóa không được để trống.");

            var success = await _collectionService.RemoveItemsFromCollectionAsync(id, itemIds);

            if (!success)
                return NotFound("Không tìm thấy bộ sưu tập hoặc món đồ không tồn tại trong bộ sưu tập.");

            return Ok(new { message = "Đã xóa món đồ khỏi bộ sưu tập." });
        }
    }
}
