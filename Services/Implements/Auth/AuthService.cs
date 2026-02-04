using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
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
    internal class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IAccountRepository accountRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _configuration = configuration;
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

            var newAccount = new Account
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow,
                Status = "Active"
            };

            await _accountRepository.SignUp(newAccount);

            return new AuthResponse { Success = true, Message = "Đăng ký thành công." };
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
                var accessToken = GenerateAccessToken(user);
                var refreshTokenEntity = GenerateRefreshToken(user.AccountId);

                await _accountRepository.AddRefreshTokenAsync(refreshTokenEntity);

                return new AuthResponse
                {
                    Success = true,
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenEntity.Token
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
