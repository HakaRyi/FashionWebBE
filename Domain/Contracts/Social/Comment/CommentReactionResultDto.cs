namespace Domain.Dto.Social.Comment
{
    public class CommentReactionResultDto
    {
        public int CommentId { get; set; }

        public bool IsLiked { get; set; }

        public int LikeCount { get; set; }
    }
}