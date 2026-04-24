using Application.Request.AccountReq;
using Application.Response.AccountRep;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> VerifyAccountAsync(string email, string code);
        Task<AuthResponse> LogoutAsync();
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<AuthResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
        Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request);
    }
}
