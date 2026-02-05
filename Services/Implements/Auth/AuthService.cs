using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.WardrobeRepos;
using Services.Helpers;
using Services.Request.AccountReq;
using Services.Response.AccountRep;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IWardrobeRepository wardrobeRepository;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AuthService(IAccountRepository accountRepository, IWardrobeRepository wardrobeRepository, IConfiguration configuration, EmailService emailService)
        {
            _accountRepository = accountRepository;
            this.wardrobeRepository = wardrobeRepository;
            _configuration = configuration;
            _emailService = emailService;
        }
        //-------------------------------------------------------------------------------------------------------------------------------//
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _accountRepository.GetAccountByEmail(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse { Success = false, Message = "Email đã tồn tại." };
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            String verificationCode = new Random().Next(1000, 9999).ToString();

            var newAccount = new Account
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow,
                Status = "Unverified",

                VerificationCode = verificationCode,
                CodeExpiredAt = DateTime.UtcNow.AddMinutes(5)
            };

            await _accountRepository.SignUp(newAccount);
            _ = _emailService.SendVerificationEmail(request.Email, verificationCode);

            return new AuthResponse { Success = true, Message = "Đăng ký thành công. Vui lòng kiểm tra Email để lấy mã xác thực." };
        }

        public async Task<AuthResponse> VerifyAccountAsync(string email, string code)
        {
            var user = await _accountRepository.GetAccountByEmail(email);

            if (user == null)
                return new AuthResponse { Success = false, Message = "Email không tồn tại." };

            if (user.Status == "Active")
                return new AuthResponse { Success = false, Message = "Tài khoản này đã được kích hoạt trước đó." };

            if (user.VerificationCode != code)
                return new AuthResponse { Success = false, Message = "Mã xác thực không đúng." };

            if (user.CodeExpiredAt < DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = "Mã xác thực đã hết hạn. Vui lòng đăng ký lại." };

            user.Status = "Active";
            user.VerificationCode = null;
            user.CodeExpiredAt = null;

            await _accountRepository.UpdateAccount(user);
            var createWardrobe = new Repositories.Entities.Wardrobe
            {
                AccountId = user.AccountId,
                Name = $"Tủ đồ của {user.Username}",
                CreatedAt = DateTime.UtcNow
            };
            await wardrobeRepository.CreateWardrobe(createWardrobe);

            return new AuthResponse { Success = true, Message = "Xác thực thành công. Bạn có thể đăng nhập ngay bây giờ." };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _accountRepository.GetAccountByEmail(request.Email);
                if (user == null)
                {
                    return new AuthResponse { Success = false, Message = "Email hoặc Mật Khẩu không chính xác !" };
                }
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    return new AuthResponse { Success = false, Message = "Email hoặc Mật Khẩu không chính xác !" };
                }
                if (user.Status != "Active")
                {
                    return new AuthResponse { Success = false, Message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email." };
                }

                var accessToken = GenerateAccessToken(user);
                var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                var existingToken = await _accountRepository.GetRefreshTokenByAccountIdAsync(user.AccountId);

                if (existingToken != null)
                {
                    existingToken.Token = refreshTokenString;
                    existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                    existingToken.CreatedAt = DateTime.UtcNow;

                    await _accountRepository.UpdateRefreshTokenAsync(existingToken);
                }
                else
                {
                    var newToken = new RefreshToken
                    {
                        Token = refreshTokenString,
                        AccountId = user.AccountId,
                        ExpiryDate = DateTime.UtcNow.AddDays(7),
                        CreatedAt = DateTime.UtcNow,
                        DeviceInfo = "Unknown",
                        IpAddress = "Unknown",
                        IsAvailable = true
                    };

                    await _accountRepository.AddRefreshTokenAsync(newToken);
                }

                return new AuthResponse
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenString
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private string GenerateAccessToken(Account user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.AccountId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(int accountId)
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                AccountId = accountId,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                DeviceInfo = "Unknown",
                IpAddress = "Unknown",
                IsAvailable = true
            };
        }
        //-------------------------------------------------------------------------------------------------------------------------------//

    }
}
