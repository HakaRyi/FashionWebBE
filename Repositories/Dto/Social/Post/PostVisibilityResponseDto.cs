using Repositories.Constants;

namespace Repositories.Dto.Social.Post
{
    public class PostVisibilityResponseDto
    {
        public int PostId { get; set; }

        public string? Status { get; set; }

        public string Visibility { get; set; } = PostVisibility.Visible;

        public bool IsPubliclyVisible { get; set; }

        public string Message { get; set; } = null!;
    }
}