namespace Domain.Entities
{
    public class PostMention
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public int MentionedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Post Post { get; set; } = null!;

        public virtual Account MentionedUser { get; set; } = null!;
    }
}