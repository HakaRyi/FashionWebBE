using Application.Services.Follow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Presentation.Controllers
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

        [HttpGet("get-followers")]
        public async Task<IActionResult> GetAllFollowers()
        {
            var userIdClaim = User.FindFirst("AccountId")?.Value
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

        [HttpGet("get-shareable-users")]
        [Authorize]
        public async Task<IActionResult> GetShareableUsers()
        {
            var userIdClaim = User.FindFirst("AccountId")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Token không chứa AccountID hợp lệ." });
            }

            var result = await service.GetShareableUsersAsync(int.Parse(userIdClaim));
            return Ok(result);
        }
    }
}
