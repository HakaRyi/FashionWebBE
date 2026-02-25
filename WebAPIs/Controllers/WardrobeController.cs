using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Wardrobe;
using Services.Request.WardrobeReq;

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WardrobeController : ControllerBase
    {
        private readonly IWardrobeService _wardrobeService;
        public WardrobeController(IWardrobeService wardrobeService)
        {
            _wardrobeService = wardrobeService;
        }
        [HttpGet("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _wardrobeService.GetAll();
            if(result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new
                {
                    message = "ko thay tu do keke"
                });
            }
        }

        [HttpGet("getById/{id}")]
        public async Task<ActionResult> Details(int id)
        {
            var result = await _wardrobeService.GetById(id);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new
                {
                    message = "ko thay tu do keke"
                });
            }
        }

        [HttpPost("createWardrobe")]
        [Authorize]
        public async Task<ActionResult> Create(WardrobeRequest wardrobeRequest)
        {
            var wardrobe = await _wardrobeService.Create(wardrobeRequest);
            if(wardrobe > 0)
            {
                return Ok(wardrobe);
            }
            else
            {
                return NotFound(new
                {
                    message = "ko tao dc tu do"
                });
            }

        }

       
    }
}
