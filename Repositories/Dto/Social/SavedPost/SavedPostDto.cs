namespace Repositories.Dto.Social.SavedPost
{
    public class SavedPostDto
    {
        public int PostId { get; set; }
        public int AccountId { get; set; }

        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }

        public string? Title { get; set; }
        public string? Content { get; set; }

        public List<string> Images { get; set; } = new();

        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int ShareCount { get; set; }

        public bool IsLiked { get; set; }
        public bool IsSaved { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime SavedAt { get; set; }
    }
}