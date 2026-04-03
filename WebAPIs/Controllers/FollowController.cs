using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Implements.Follow;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService service;
        public FollowController(IFollowService service)
        {
            this.service = service;
        }
        // GET: api/<FollowController>
        [HttpGet("get-followers")]
        public async Task<IActionResult> GetAllFollowers()
        {
            var userIdClaim = User.FindFirst("AccountId")?.Value
                      // Nếu không thấy, thử lấy theo tên dài chuẩn Microsoft
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine(User.Identity?.IsAuthenticated);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Token không chứa AccountID hợp lệ." });
            }
            var result = await service.GetFollowersByIdAsync(int.Parse(userIdClaim));
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new
                {
                    message = "da co loi xay ra"
                });
            }
        }
        [HttpGet("get-followings")]
        public async Task<IActionResult> GetAllFollowings()
        {
            var userIdClaim = User.FindFirst("AccountId")?.Value
                      // Nếu không thấy, thử lấy theo tên dài chuẩn Microsoft
                      ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine(User.Identity?.IsAuthenticated);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Token không chứa AccountID hợp lệ." });
            }
            var result = await service.GetFollowingsByIdAsync(int.Parse(userIdClaim));
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new
                {
                    message = "da co loi xay ra"
                });
            }
        }

        // GET api/<FollowController>/5
        [HttpGet("{followerId}")]
        public async Task<IActionResult> GetById([FromRoute] int followerId)
        {
            var userId = User.FindFirst("AccountId")?.Value;
            var result = await service.GetFollowerByIdAsync(int.Parse(userId), followerId);
            if (result != null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new
                {
                    message = "da co loi xay ra"
                });
            }
        }


        // POST api/<FollowController>
        [HttpPost("{followerId}")]
        [Authorize]
        public async Task<IActionResult> Post([FromRoute] int followerId)
        {
            var userId = User.FindFirst("AccountId")?.Value;
            var result = await service.FollowUserAsync(int.Parse(userId), followerId);
            if (result)
            {
                return Ok(new
                {
                    message = "Follow thanh cong"
                });
            }
            else
            {
                return BadRequest(new
                {
                    message = "da co loi xay ra"
                });
            }
        }

        [HttpPost("check/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> CheckFollow([FromRoute] int targetUserId)
        {
            var currentUserIdStr = User.FindFirst("AccountId")?.Value;

            if (string.IsNullOrEmpty(currentUserIdStr))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            var followerId = int.Parse(currentUserIdStr);

            var result = await service.IsFollowingAsync(followerId, targetUserId);

            return Ok(new
            {
                isFollowing = result
            });
        }
        // DELETE api/<FollowController>/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst("AccountId")?.Value;
            var result = await service.UnfollowUserAsync(int.Parse(userId), id);
            if (result)
            {
                return Ok(new
                {
                    message = "Unfollow thanh cong"
                });
            }
            else
            {
                return BadRequest(new
                {
                    message = "da co loi xay ra"
                });
            }
        }
    }
}
