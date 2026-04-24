using Application.Interfaces;
using Application.Request.AccountReq;
using Application.Response.AccountRep;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/userProfileController")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Lấy thông tin hồ sơ của người dùng hiện tại
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserProfileResponseDto>> GetMyProfile()
        {
            var profile = await _userProfileService.GetUserProfileAsync();

            if (profile == null)
            {
                return NotFound(new { message = "User information not found." });
            }

            return Ok(profile);
        }

        /// <summary>
        /// API hoàn tất Onboarding (chỉ gọi 1 lần duy nhất sau khi đăng ký)
        /// </summary>
        [HttpPost("complete-onboarding")]
        public async Task<IActionResult> CompleteOnboarding([FromBody] OnboardingRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _userProfileService.CompleteOnboardingAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "Unable to complete onboarding. Please try again." });
            }

            return Ok(new { message = "Onboarding completed successfully!" });
        }

        /// <summary>
        /// API cập nhật thông tin hồ sơ (Dùng cho trang Edit Profile)
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _userProfileService.UpdateUserProfileAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "The record update failed." });
            }

            return Ok(new { message = "Profile updated successfully!" });
        }
    }
}
