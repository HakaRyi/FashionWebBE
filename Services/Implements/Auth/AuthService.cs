using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.WalletRepos;
using Repositories.Repos.WardrobeRepos;
using Services.Helpers;
using Services.Request.AccountReq;
using Services.Response.AccountRep;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implements.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<Account> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IWardrobeRepository _wardrobeRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IAccountRepository _accountRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWalletRepository _walletRepository;

        public AuthService(
            UserManager<Account> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IWardrobeRepository wardrobeRepository,
            IAccountRepository accountRepository,
            IConfiguration configuration,
            EmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IWalletRepository walletRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _wardrobeRepository = wardrobeRepository;
            _accountRepository = accountRepository;
            _configuration = configuration;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _walletRepository = walletRepository;
        }

        #region Helper Methods
        private string GetClientDeviceInfo()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? "Unknown Device" : userAgent;
        }

        private string GetClientIpAddress()
        {
            // Kiểm tra Header X-Forwarded-For (dành cho môi trường có Proxy/Nginx)
            var ip = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();

            if (string.IsNullOrEmpty(ip))
            {
                // Lấy trực tiếp từ connection
                ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            }

            return string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
        }

        private string GenerateRandomVerificationCode() => new Random().Next(100000, 999999).ToString();
        #endregion

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };

            var verificationCode = GenerateRandomVerificationCode();

            var newAccount = new Account
            {
                UserName = request.Username,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                Status = "Unverified",
                VerificationCode = verificationCode,
                CodeExpiredAt = DateTime.UtcNow.AddMinutes(15),
                FreeTryOn = 3,
                CountPost = 0,
                CountFollower = 0,
                CountFollowing = 0
            };


            var result = await _userManager.CreateAsync(newAccount, request.Password);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Đăng ký thất bại.";
                return new AuthResponse { Success = false, Message = error };
            }

            await _userManager.AddToRoleAsync(newAccount, "User");
            await _walletRepository.CreateWalletAsync(new Wallet
            {
                AccountId = newAccount.Id,
                Balance = 0,
                LockedBalance = 0,
                UpdatedAt = DateTime.UtcNow,
                Currency = "VND"
            });
            try
            {
                await _emailService.SendVerificationEmail(request.Email, verificationCode);
            }
            catch (Exception)
            {
                return new AuthResponse { Success = true, Message = "Đăng ký thành công nhưng gửi email xác thực thất bại. Vui lòng yêu cầu gửi lại sau." };
            }

            return new AuthResponse { Success = true, Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản." };
        }

        public async Task<AuthResponse> VerifyAccountAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản." };
            if (user.Status == "Active") return new AuthResponse { Success = false, Message = "Tài khoản đã được xác thực trước đó." };
            if (user.VerificationCode != code) return new AuthResponse { Success = false, Message = "Mã xác thực không chính xác." };
            if (user.CodeExpiredAt < DateTime.UtcNow) return new AuthResponse { Success = false, Message = "Mã xác thực đã hết hạn." };

            user.Status = "Active";
            user.EmailConfirmed = true;
            user.VerificationCode = null;
            user.CodeExpiredAt = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return new AuthResponse { Success = false, Message = "Có lỗi xảy ra khi cập nhật trạng thái." };

            // Khởi tạo tủ đồ mặc định
            await _wardrobeRepository.CreateWardrobe(new Repositories.Entities.Wardrobe
            {
                AccountId = user.Id,
                Name = $"Tủ đồ của {user.UserName}",
                CreatedAt = DateTime.UtcNow
            });

            return new AuthResponse { Success = true, Message = "Xác thực tài khoản thành công." };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return new AuthResponse { Success = false, Message = "Email hoặc mật khẩu không chính xác." };
            }

            if (user.Status != "Active")
                return new AuthResponse { Success = false, Message = "Tài khoản chưa được xác thực email." };

            var accessToken = await GenerateAccessToken(user);
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // Thu thập thông tin Client
            var deviceInfo = GetClientDeviceInfo();
            var ipAddress = GetClientIpAddress();

            var existingToken = await _accountRepository.GetRefreshTokenByAccountIdAsync(user.Id);

            if (existingToken != null)
            {
                existingToken.Token = refreshTokenString;
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                existingToken.CreatedAt = DateTime.UtcNow;
                existingToken.DeviceInfo = deviceInfo;
                existingToken.IpAddress = ipAddress; // CẬP NHẬT IP
                existingToken.IsAvailable = true;
                await _accountRepository.UpdateRefreshTokenAsync(existingToken);
            }
            else
            {
                await _accountRepository.AddRefreshTokenAsync(new RefreshToken
                {
                    Token = refreshTokenString,
                    AccountId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IsAvailable = true,
                    DeviceInfo = deviceInfo,
                    IpAddress = ipAddress
                });
            }

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                Message = "Đăng nhập thành công."
            };
        }

        private async Task<string> GenerateAccessToken(Account user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("AccountId", user.Id.ToString()),
                new Claim("Username", user.UserName)
            };

            //if (!string.IsNullOrEmpty(user.Avatar))
            //    claims.Add(new Claim("Avatar", user.Avatar));

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}