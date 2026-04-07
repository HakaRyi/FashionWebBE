namespace Domain.Dto.Social.Comment
{
    public class ReplyCommentDto
    {
        public int ParentCommentId { get; set; }

        public string Content { get; set; } = null!;
    }
}
