namespace Services.Response.CommentResp
{
    public class CommentResponse
    {
        public int CommentId { get; set; }

        public int PostId { get; set; }

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int AccountId { get; set; }

        public string Username { get; set; } = null!;
    }
}