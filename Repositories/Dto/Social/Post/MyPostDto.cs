using Repositories.Constants;

namespace Repositories.Dto.Social.Post
{
    public class MyPostDto
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

        public string? Status { get; set; }

        public string Visibility { get; set; } = PostVisibility.Visible;

        public bool IsOwner { get; set; } = true;

        public bool CanEdit { get; set; }

        public bool CanDelete { get; set; }

        public bool CanHide { get; set; }

        public bool CanUnhide { get; set; }

        public bool IsPubliclyVisible { get; set; }
    }
}