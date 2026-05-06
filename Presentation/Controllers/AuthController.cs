using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Request.AccountReq;
using Application.Response.AccountRep;

namespace Presentation.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }

            SetTokenCookie("accessToken", result.AccessToken);
            SetTokenCookie("refreshToken", result.RefreshToken);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                user = new
                {
                    email = request.Email
                }
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var refreshToken = request.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");

                return BadRequest(new
                {
                    message = "Refresh token is required."
                });
            }

            var result = await _authService.LogoutAsync(refreshToken);

            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            if (!result.Success)
            {
                return BadRequest(new
                {
                    message = result.Message
                });
            }

            return Ok(new
            {
                message = "Logout successful."
            });
        }

        // HELPER METHODS
        // ======================================================
        private void SetTokenCookie(string key, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),

                Secure = false,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append(key, value, cookieOptions);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyRequest request)
        {
            var result = await _authService.VerifyAccountAsync(request.Email, request.Code);

            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return BadRequest(new { message = "Refresh Token không được để trống." });

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            SetTokenCookie("accessToken", result.AccessToken);
            SetTokenCookie("refreshToken", result.RefreshToken);

            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }

        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; } = null!;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var authResponse = await _authService.LoginWithGoogleAsync(request);
                return Ok(new
                {
                    success = true,
                    accessToken = authResponse.AccessToken,
                    refreshToken = authResponse.RefreshToken,
                    data = authResponse
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Xác thực Google thất bại: " + ex.Message
                });
            }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ChangePasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
