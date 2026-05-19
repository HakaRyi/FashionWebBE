namespace Domain.Entities
{
    public class Hashtag
    {
        public int HashtagId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int UsageCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    }
}