using Microsoft.AspNetCore.Mvc;
using Services.Implements.Auth;
using Services.Request.AccountReq;

namespace WebAPIs.Controllers
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

        // API Đăng nhập: POST api/WAuth/login
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

            // Mobile sẽ đọc field accessToken và lưu vào Secure Storage
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
        public IActionResult Logout()
        {
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Đăng xuất thành công" });
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

                // Bắt buộc dùng HTTPS (Nếu chạy localhost không có https thì set false tạm)
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
    }
}
