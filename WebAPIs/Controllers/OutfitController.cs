using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.OutfitImp;
using Services.Request.OufitReq;
using System.Security.Claims;

namespace WebAPIs.Controllers
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
    }
}
