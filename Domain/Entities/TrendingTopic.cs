namespace Domain.Entities
{
    public class TrendingTopic
    {
        public int Id { get; set; }

        public string Keyword { get; set; } = string.Empty;

        public double Score { get; set; }

        public int TotalPosts { get; set; }

        public int TotalEngagement { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}