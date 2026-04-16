
using Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Application.Helpers;
using Application.Request.AccountReq;
using Application.Response.AccountRep;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
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
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageRepository _imageRepository;

        public AuthService(
            UserManager<Account> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IWardrobeRepository wardrobeRepository,
            IAccountRepository accountRepository,
            IConfiguration configuration,
            EmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IWalletRepository walletRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            IImageRepository imageRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _wardrobeRepository = wardrobeRepository;
            _accountRepository = accountRepository;
            _configuration = configuration;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _walletRepository = walletRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _imageRepository = imageRepository;
        }

        #region Helper Methods

        private string GetClientDeviceInfo()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? "Unknown Device" : userAgent;
        }

        private string GetClientIpAddress()
        {
            var ip = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();

            if (string.IsNullOrEmpty(ip))
            {
                ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            }

            return string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
        }

        private string GenerateRandomVerificationCode()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        #endregion

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
            }

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                return new AuthResponse { Success = false, Message = "Tên đăng nhập này đã tồn tại." };
            }

            if (request.DateOfBirth > DateTime.UtcNow.AddYears(-13))
            {
                return new AuthResponse { Success = false, Message = "Bạn phải trên 13 tuổi để đăng ký." };
            }

            var verificationCode = GenerateRandomVerificationCode();

            var newAccount = new Account
            {
                UserName = request.Username,
                Email = request.Email,
                DateOfBirth = request.DateOfBirth,
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
                return new AuthResponse
                {
                    Success = false,
                    Message = error
                };
            }

            await _userManager.AddToRoleAsync(newAccount, "User");

            try
            {
                await _emailService.SendVerificationEmail(request.Email, verificationCode);
            }
            catch (Exception)
            {
                return new AuthResponse
                {
                    Success = true,
                    Message = "Đăng ký thành công nhưng gửi email xác thực thất bại. Vui lòng yêu cầu gửi lại sau."
                };
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản."
            };
        }

        public async Task<AuthResponse> VerifyAccountAsync(string email, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Không tìm thấy tài khoản."
                };
            }

            if (user.Status == "Active")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản đã được xác thực trước đó."
                };
            }

            if (user.VerificationCode != code)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Mã xác thực không chính xác."
                };
            }

            if (user.CodeExpiredAt < DateTime.UtcNow)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Mã xác thực đã hết hạn."
                };
            }

            user.Status = "Active";
            user.EmailConfirmed = true;
            user.VerificationCode = null;
            user.CodeExpiredAt = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi cập nhật trạng thái."
                };
            }

            var existingWallet = await _walletRepository.GetByAccountIdAsync(user.Id);
            if (existingWallet == null)
            {
                await _walletRepository.AddAsync(new Wallet
                {
                    AccountId = user.Id,
                    Balance = 0,
                    LockedBalance = 0,
                    Currency = "VND",
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _wardrobeRepository.CreateWardrobe(new Domain.Entities.Wardrobe
            {
                AccountId = user.Id,
                Name = $"Tủ đồ của {user.UserName}",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Xác thực tài khoản thành công."
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email hoặc mật khẩu không chính xác."
                };
            }

            user.IsOnline = "Online";

            if (user.Status != "Active")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản chưa được xác thực email."
                };
            }

            await _userManager.UpdateAsync(user);

            var accessToken = await GenerateAccessToken(user);
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var deviceInfo = GetClientDeviceInfo();
            var ipAddress = GetClientIpAddress();

            var existingToken = await _accountRepository.GetRefreshTokenByAccountIdAsync(user.Id);

            if (existingToken != null)
            {
                existingToken.Token = refreshTokenString;
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                existingToken.CreatedAt = DateTime.UtcNow;
                existingToken.DeviceInfo = deviceInfo;
                existingToken.IpAddress = ipAddress;
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

        public async Task<AuthResponse> LogoutAsync()
        {
            var accountIdClaim = _currentUserService.GetUserId() ?? 0;
            if (accountIdClaim == 0)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Không tìm thấy thông tin tài khoản."
                };
            }

            var user = await _userManager.FindByIdAsync(accountIdClaim.ToString());
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Tài khoản không tồn tại."
                };
            }

            user.IsOnline = "Offline";
            await _userManager.UpdateAsync(user);

            var refreshToken = await _accountRepository.GetRefreshTokenByAccountIdAsync(accountIdClaim);
            if (refreshToken != null)
            {
                refreshToken.IsAvailable = false;
                await _accountRepository.UpdateRefreshTokenAsync(refreshToken);
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng xuất thành công."
            };
        }

        private async Task<string> GenerateAccessToken(Account user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var avatarUrl = user.Avatars?.FirstOrDefault()?.ImageUrl ?? "";

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("AccountId", user.Id.ToString()),
                new Claim("Username", user.UserName ?? string.Empty),
                new Claim("Avatar", avatarUrl)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshTokenString)
        {
            // 1. Tìm token trong DB thông qua Repository
            var storedToken = await _accountRepository.GetRefreshTokenByTokenAsync(refreshTokenString);

            if (storedToken == null)
                return new AuthResponse { Success = false, Message = "Refresh token không tồn tại." };

            if (!storedToken.IsAvailable == true)
                return new AuthResponse { Success = false, Message = "Refresh token đã bị vô hiệu hóa." };

            if (storedToken.ExpiryDate < DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = "Refresh token đã hết hạn." };

            // 2. Lấy thông tin User sở hữu token này
            var user = await _userManager.FindByIdAsync(storedToken.AccountId.ToString());
            if (user == null)
                return new AuthResponse { Success = false, Message = "Không tìm thấy người dùng." };

            // 3. Tạo Access Token mới và Refresh Token mới (Rotation)
            var newAccessToken = await GenerateAccessToken(user);
            var newRefreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            // 4. Cập nhật lại Token cũ trong DB thành Token mới
            storedToken.Token = newRefreshTokenString;
            storedToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
            storedToken.CreatedAt = DateTime.UtcNow;
            storedToken.IpAddress = GetClientIpAddress();
            storedToken.DeviceInfo = GetClientDeviceInfo();

            await _accountRepository.UpdateRefreshTokenAsync(storedToken);

            return new AuthResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                Message = "Làm mới token thành công."
            };
        }

        public async Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };

            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch
            {
                throw new Exception("Google token không hợp lệ");
            }

            if (!payload.EmailVerified)
            {
                throw new Exception("Email chưa được xác thực bởi Google");
            }
            bool isNewUser = false;
            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                isNewUser = true;
                user = new Account
                {
                    UserName = $"{payload.Email.Split('@')[0]}_{Guid.NewGuid():N}".Substring(0, 20),
                    Email = payload.Email,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active",
                    FreeTryOn = 3,
                    CountPost = 0,
                    CountFollower = 0,
                    CountFollowing = 0,
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                };

                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    if (!string.IsNullOrEmpty(payload.Picture))
                    {
                        user.Avatars.Add(new Image
                        {
                            ImageUrl = payload.Picture,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception(errors);
                }
                await _userManager.AddToRoleAsync(user, "User");

                await _walletRepository.AddAsync(new Wallet
                {
                    AccountId = user.Id,
                    Balance = 0,
                    LockedBalance = 0,
                    Currency = "VND",
                    UpdatedAt = DateTime.UtcNow
                });

                await _wardrobeRepository.CreateWardrobe(new Domain.Entities.Wardrobe
                {
                    AccountId = user.Id,
                    Name = $"Tủ đồ của {user.UserName}",
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync();
            }

            user.IsOnline = "Online";
            await _userManager.UpdateAsync(user);
            var accessToken = await GenerateAccessToken(user);
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var deviceInfo = GetClientDeviceInfo();
            var ipAddress = GetClientIpAddress();

            var existingToken = await _accountRepository.GetRefreshTokenByAccountIdAsync(user.Id);

            if (existingToken != null)
            {
                existingToken.Token = refreshTokenString;
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                existingToken.CreatedAt = DateTime.UtcNow;
                existingToken.DeviceInfo = deviceInfo;
                existingToken.IpAddress = ipAddress;
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
                Message = "Đăng nhập thành công.",
                IsNewUser = isNewUser
            };
        }
    }
}