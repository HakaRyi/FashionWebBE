using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Auth;
using Services.Implements.Wardrobe;
using Services.Request.WardrobeReq;

namespace WebAPIs.Controllers
{
    [ApiController]
    [Route("api/wardrobes")]
    public class WardrobeController : ControllerBase
    {
        private readonly IWardrobeService _wardrobeService;
        private readonly ICurrentUserService _currentUserService;

        public WardrobeController(
            IWardrobeService wardrobeService,
            ICurrentUserService currentUserService)
        {
            _wardrobeService = wardrobeService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _wardrobeService.GetAllAsync();

            return Ok(new
            {
                message = "Lấy danh sách tủ đồ thành công.",
                data = result
            });
        }

        [HttpGet("account/{accountId:int}")]
        public async Task<IActionResult> GetByAccountId(int accountId)
        {
            var result = await _wardrobeService.GetByAccountIdAsync(accountId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy tủ đồ."
                });
            }

            return Ok(new
            {
                message = "Lấy thông tin tủ đồ thành công.",
                data = result
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WardrobeRequest wardrobeRequest)
        {
            try
            {
                var result = await _wardrobeService.CreateAsync(wardrobeRequest);

                return Ok(new
                {
                    message = "Tạo tủ đồ thành công.",
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
                    message = "Có lỗi hệ thống khi tạo tủ đồ.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("me/items")]
        public async Task<IActionResult> GetMyWardrobeItems()
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var result = await _wardrobeService.GetMyWardrobeItemsAsync(accountId);

            return Ok(new
            {
                message = "Lấy danh sách item trong tủ đồ của tôi thành công.",
                data = result
            });
        }

        [HttpGet("public/{accountId:int}/profile")]
        public async Task<IActionResult> GetPublicProfile(int accountId)
        {
            var result = await _wardrobeService.GetPublicProfileAsync(accountId);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy người dùng."
                });
            }

            return Ok(new
            {
                message = "Lấy thông tin trang cá nhân công khai thành công.",
                data = result
            });
        }

        [HttpGet("public/{accountId:int}/items")]
        public async Task<IActionResult> GetPublicWardrobeItems(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            var result = await _wardrobeService.GetPublicWardrobeAsync(accountId, page, pageSize);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy người dùng hoặc tủ đồ."
                });
            }

            return Ok(new
            {
                message = "Lấy danh sách item public thành công.",
                data = result
            });
        }
    }
}