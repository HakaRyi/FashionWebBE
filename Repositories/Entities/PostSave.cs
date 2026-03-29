namespace Repositories.Entities
{
    public class PostSave
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Post Post { get; set; } = null!;

        public Account Account { get; set; } = null!;
    }
}
