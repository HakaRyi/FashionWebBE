using Application.Interfaces;
using System.Security.Claims;

namespace Presentation.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var rawUserId =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("AccountId") ??
                user.FindFirstValue("Id") ??
                user.FindFirstValue("sub");

            return int.TryParse(rawUserId, out var id) ? id : null;
        }

        public int GetRequiredUserId()
        {
            var id = GetUserId();
            if (!id.HasValue)
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ.");

            return id.Value;
        }

        public string? GetEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email");
        }

        public string? GetUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null) return null;

            return user.FindFirstValue("Username")
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("username");
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}