namespace Services.Response.AccountRep
{
    public class FashionExpertDetail
    {
        public int AccountId { get; set; }
        public int ExpertProfileId { get; set; }

        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int RoleId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }
        public string? Avatar { get; set; }
        public int? YearsOfExperience { get; set; }

        public string? Bio { get; set; }

        public bool? Verified { get; set; }
        public int FollowerCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostCount { get; set; }
        public string? Description { get; set; }


        public DateTime? CreatedAtProfile { get; set; }

        public DateTime? UpdatedAtProfile { get; set; }
    }
}
