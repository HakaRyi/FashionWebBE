namespace Application.Response.AccountRep
{
    public class AccountResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Role { get; set; }
        public string Status { get; set; } = string.Empty;
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostCount { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? IsOnline { get; set; }
    }
}
