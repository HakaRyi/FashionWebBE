using Application.Services.OutfitImp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Request.OufitReq;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutfitController : ControllerBase
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOutfit([FromForm] AddOutfitRequest request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (accountIdClaim == null || !int.TryParse(accountIdClaim.Value, out int accountId))
                {
                    return Unauthorized();
                }

                var result = await _outfitService.CreateOutfitAsync(accountId, request.OutfitName, request.Image);

                return Ok(new { message = "Lưu outfit thành công!", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveOutfit([FromBody] SaveOutfitRequestDto request)
        {
            try
            {
                var result = await _outfitService.SaveOutfitAsync(request);
                return Ok(new { Message = "Lưu bộ đồ thành công!", Data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-outfits")]
        public async Task<IActionResult> GetMyOutfits()
        {
            try
            {
                var result = await _outfitService.GetMyOutfitsAsync();
                return Ok(new { Message = "Lấy danh sách bộ đồ thành công!", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống.", Error = ex.Message });
            }
        }
    }
}
