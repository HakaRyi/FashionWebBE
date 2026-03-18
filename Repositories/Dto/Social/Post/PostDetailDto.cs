using Repositories.Dto.Social.Comment;

namespace Repositories.Dto.Social.Post
{
    public class PostDetailDto
    {
        public int PostId { get; set; }

        public int AccountId { get; set; }

        public string UserName { get; set; } = null!;

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

        public List<CommentDto> Comments { get; set; } = new();
    }
}
