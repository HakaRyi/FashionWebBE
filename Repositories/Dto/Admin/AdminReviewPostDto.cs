namespace Repositories.Dto.Social.Post
{
    public class AdminReviewPostDto
    {
        public int PostId { get; set; }

        public int AccountId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public List<string> Images { get; set; } = new();

        public string? Status { get; set; }

        public string Visibility { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}