namespace Domain.Contracts.Social.Trend
{
    public class TrendingTopicDto
    {
        public int HashtagId { get; set; }

        public string Keyword { get; set; } = string.Empty;

        public double Score { get; set; }

        public int TotalPosts { get; set; }

        public int TotalEngagement { get; set; }

        public DateTime CalculatedAt { get; set; }
    }

    public class TrendingTopicSummaryDto
    {
        public List<TrendingTopicDto> Items { get; set; } = new();

        public DateTime GeneratedAt { get; set; }

        public string Scope { get; set; } = "user";
    }
}