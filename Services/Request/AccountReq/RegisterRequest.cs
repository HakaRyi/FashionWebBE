using System.ComponentModel.DataAnnotations;

namespace Services.Request.AccountReq
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public int RoleId { get; set; }
    }
}
