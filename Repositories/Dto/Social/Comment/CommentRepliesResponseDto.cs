namespace Repositories.Dto.Social.Comment
{
    public class CommentRepliesResponseDto
    {
        public int ParentCommentId { get; set; }

        public int ReplyCount { get; set; }

        public bool HasReplies { get; set; }

        public List<CommentReplyDto> Items { get; set; } = new();

        public int Skip { get; set; }

        public int Take { get; set; }

        public bool HasMore { get; set; }
    }
}