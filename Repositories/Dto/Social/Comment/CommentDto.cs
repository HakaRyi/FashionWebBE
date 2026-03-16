namespace Repositories.Dto.Social.Comment
{
    public class CommentDto
    {
        public int CommentId { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public string UserName { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string Content { get; set; } = null!;

        public int LikeCount { get; set; }

        public bool IsLiked { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? ParentCommentId { get; set; }

        public int ReplyCount { get; set; }

        public bool HasReplies { get; set; }

        public List<CommentReplyDto> Replies { get; set; } = new();
    }
}