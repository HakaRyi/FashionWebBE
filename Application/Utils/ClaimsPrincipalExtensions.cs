using System.Security.Claims;

namespace Application.Utils
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new UnauthorizedAccessException("Invalid token.");

            if (!int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Invalid user id in token.");

            return userId;
        }
    }
}