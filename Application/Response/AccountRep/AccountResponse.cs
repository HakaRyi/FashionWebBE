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

    public class AccountUserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Avatar { get; set; }
        public string Role { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Status { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostCount { get; set; }
        public string? Description { get; set; }
        public string? IsOnline { get; set; }

        // --- Thông tin bổ sung cho Expert ---
        public bool IsExpert { get; set; }
        public int? ReputationScore { get; set; }
        public string? ExpertiseField { get; set; }
        public int? YearsOfExperience { get; set; }
        public double? Rating { get; set; }
        public string? Bio { get; set; }
        public bool? Verified { get; set; }
    }
}
