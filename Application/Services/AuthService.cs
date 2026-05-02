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

        private string GetClientDeviceInfo()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? "Unknown Device" : userAgent;
        }

        private string GetClientIpAddress()
        {
            var ip = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();

            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip.Split(',')[0].Trim();
            }

            ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            return string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip;
        }

        private string GenerateRandomVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private int GetVerificationCodeExpiryMinutes()
        {
            return int.Parse(_configuration["Auth:VerificationCodeExpiryMinutes"] ?? "15");
        }

        private int GetRefreshTokenExpiryDays()
        {
            return int.Parse(_configuration["Auth:RefreshTokenExpiryDays"] ?? "7");
        }

        private int GetMaxActiveDevices()
        {
            return int.Parse(_configuration["Auth:MaxActiveDevices"] ?? "5");
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return new AuthResponse { Success = false, Message = "This email is already in use." };
            }

            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                return new AuthResponse { Success = false, Message = "This username already exists." };
            }

            if (request.DateOfBirth > DateTime.UtcNow.AddYears(-13))
            {
                return new AuthResponse { Success = false, Message = "You must be at least 13 years old to register." };
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
                CodeExpiredAt = DateTime.UtcNow.AddMinutes(GetVerificationCodeExpiryMinutes()),
                FreeTryOn = 3,
                CountPost = 0,
                CountFollower = 0,
                CountFollowing = 0
            };

            var result = await _userManager.CreateAsync(newAccount, request.Password);
            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Registration failed.";

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
                    Message = "Registration was successful, but the verification email could not be sent. Please request a new verification email later."
                };
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful. Please check your email to verify your account."
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
                    Message = "Account not found."
                };
            }

            if (user.Status == "Active")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "This account has already been verified."
                };
            }

            if (user.VerificationCode != code)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "The verification code is incorrect."
                };
            }

            if (user.CodeExpiredAt < DateTime.UtcNow)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "The verification code has expired."
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
                    Message = "An error occurred while updating the account status."
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
                Name = $"{user.UserName}'s wardrobe",
                CreatedAt = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Account verified successfully."
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
                    Message = "Incorrect email or password."
                };
            }

            if (user.Status != "Active")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "This account has not been verified by email."
                };
            }

            user.IsOnline = "Online";
            await _userManager.UpdateAsync(user);

            var accessToken = await GenerateAccessToken(user);
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var deviceInfo = GetClientDeviceInfo();
            var ipAddress = GetClientIpAddress();

            await LimitActiveDevicesAsync(user.Id);

            await _accountRepository.AddRefreshTokenAsync(new RefreshToken
            {
                Token = refreshTokenString,
                AccountId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
                CreatedAt = DateTime.UtcNow,
                IsAvailable = true,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            });

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                Message = "Login successful.",
                HasCompletedOnboarding = user.HasCompletedOnboarding
            };
        }

        public async Task<AuthResponse> LogoutAsync(string refreshTokenString)
        {
            var accountId = _currentUserService.GetUserId() ?? 0;
            if (accountId == 0)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Account information was not found."
                };
            }

            var user = await _userManager.FindByIdAsync(accountId.ToString());
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Account does not exist."
                };
            }

            var refreshToken = await _accountRepository.GetRefreshTokenByTokenAsync(refreshTokenString);
            if (refreshToken == null || refreshToken.AccountId != accountId)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Refresh token does not exist."
                };
            }

            refreshToken.IsAvailable = false;
            await _accountRepository.UpdateRefreshTokenAsync(refreshToken);

            var remainingActiveTokens = await _accountRepository.GetActiveRefreshTokensByAccountIdAsync(accountId);

            user.IsOnline = remainingActiveTokens.Any() ? "Online" : "Offline";
            await _userManager.UpdateAsync(user);

            return new AuthResponse
            {
                Success = true,
                Message = "Logout successful."
            };
        }

        public async Task<string> GenerateAccessToken(Account user)
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
                new Claim("Avatar", avatarUrl),
                new Claim("HasCompletedOnboarding", user.HasCompletedOnboarding.ToString().ToLower())
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
            var storedToken = await _accountRepository.GetRefreshTokenByTokenAsync(refreshTokenString);

            if (storedToken == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Refresh token does not exist."
                };
            }

            if (storedToken.IsAvailable != true)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Refresh token has been disabled."
                };
            }

            if (storedToken.ExpiryDate < DateTime.UtcNow)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Refresh token has expired."
                };
            }

            var user = await _userManager.FindByIdAsync(storedToken.AccountId.ToString());
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (user.Status != "Active")
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "This account is not active."
                };
            }

            var newAccessToken = await GenerateAccessToken(user);
            var newRefreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            storedToken.Token = newRefreshTokenString;
            storedToken.ExpiryDate = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays());
            storedToken.CreatedAt = DateTime.UtcNow;
            storedToken.IpAddress = GetClientIpAddress();
            storedToken.DeviceInfo = GetClientDeviceInfo();

            await _accountRepository.UpdateRefreshTokenAsync(storedToken);

            return new AuthResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                Message = "Token refreshed successfully."
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
                throw new Exception("Invalid Google token.");
            }

            if (!payload.EmailVerified)
            {
                throw new Exception("This email has not been verified by Google.");
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
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    user.Avatars.Add(new Image
                    {
                        ImageUrl = payload.Picture,
                        CreatedAt = DateTime.UtcNow
                    });
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
                    Name = $"{user.UserName}'s wardrobe",
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

            await LimitActiveDevicesAsync(user.Id);

            await _accountRepository.AddRefreshTokenAsync(new RefreshToken
            {
                Token = refreshTokenString,
                AccountId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
                CreatedAt = DateTime.UtcNow,
                IsAvailable = true,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress
            });

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                Message = "Login successful.",
                IsNewUser = isNewUser,
                HasCompletedOnboarding = user.HasCompletedOnboarding
            };
        }

        private async Task LimitActiveDevicesAsync(int accountId)
        {
            var maxActiveDevices = GetMaxActiveDevices();

            var activeTokens = await _accountRepository.GetActiveRefreshTokensByAccountIdAsync(accountId);

            if (activeTokens.Count < maxActiveDevices)
            {
                return;
            }

            var tokensToDisable = activeTokens
                .OrderBy(token => token.CreatedAt)
                .Take(activeTokens.Count - maxActiveDevices + 1)
                .ToList();

            foreach (var token in tokensToDisable)
            {
                token.IsAvailable = false;
                await _accountRepository.UpdateRefreshTokenAsync(token);
            }
        }

        public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Email is required."
                };
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "New password is required."
                };
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Account not found."
                };
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                var removeError = removePasswordResult.Errors.FirstOrDefault()?.Description
                                  ?? "Failed to remove old password.";

                return new AuthResponse
                {
                    Success = false,
                    Message = removeError
                };
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                var addError = addPasswordResult.Errors.FirstOrDefault()?.Description
                               ?? "Failed to set new password.";

                return new AuthResponse
                {
                    Success = false,
                    Message = addError
                };
            }

            user.SecurityStamp = Guid.NewGuid().ToString();
            await _userManager.UpdateAsync(user);

            var refreshTokens = await _accountRepository.GetActiveRefreshTokensByAccountIdAsync(user.Id);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.IsAvailable = false;
                await _accountRepository.UpdateRefreshTokenAsync(refreshToken);
            }

            return new AuthResponse
            {
                Success = true,
                Message = "Password changed successfully."
            };
        }
    }
}