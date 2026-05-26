namespace Domain.Entities
{
    public class PostHashtag
    {
        public int PostId { get; set; }

        public int HashtagId { get; set; }

        public Post Post { get; set; } = null!;

        public Hashtag Hashtag { get; set; } = null!;
    }
}