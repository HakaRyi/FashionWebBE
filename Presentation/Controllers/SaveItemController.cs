using Application.Services.ItemSaveImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Route("api/items")]
    [ApiController]
    public class SaveItemController : ControllerBase
    {
        private readonly IItemSaveService _itemSaveService;
        public SaveItemController(IItemSaveService itemSaveService)
        {
            _itemSaveService = itemSaveService;
        }
        [HttpPost("{itemId}/save")]
        [Authorize]
        public async Task<IActionResult> SaveItem(int itemId)
        {
            try
            {
                await _itemSaveService.SaveItem(itemId);
                return Ok(new { message = "Item saved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete("{itemId}/save")]
        [Authorize]
        public async Task<IActionResult> DeleteSaveItem(int itemId)
        {
            try
            {
                await _itemSaveService.DeleteSaveItem(itemId);
                return Ok(new { message = "Saved item deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

        }
        [HttpGet("saved")]
        [Authorize]
        public async Task<IActionResult> GetMySavedItems()
        {
            try
            {
                var savedItems = await _itemSaveService.GetMySaveItems();
                return Ok(savedItems);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
