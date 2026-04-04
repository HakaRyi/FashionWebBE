using Microsoft.AspNetCore.Http;

namespace Application.Request.AccountReq
{
    public class UpdateAccountRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public int Role { get; set; }
        public string? Description { get; set; }    
        public string? Status { get; set; }
    }
    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public IFormFile? Avatar { get; set; }
        public string? Description { get; set; }
    }
}
