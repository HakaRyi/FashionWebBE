using Services.Request.AccountReq;
using Services.Response.AccountRep;

namespace Services.Implements.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> VerifyAccountAsync(string email, string code);
        Task<AuthResponse> LogoutAsync();
    }
}
