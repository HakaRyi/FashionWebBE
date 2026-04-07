namespace Domain.Dto.Social.Comment
{
    public class CreateReplyResultDto
    {
        public CommentReplyDto Reply { get; set; } = null!;

        public int ParentCommentId { get; set; }

        public int ReplyCount { get; set; }

        public bool HasReplies { get; set; }
    }
}