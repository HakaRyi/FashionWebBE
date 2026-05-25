namespace Domain.Contracts.Social.Trend
{
    public class PostTrendDto
    {
        public int PostId { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public string UserName { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public double Score { get; set; }

        public double EngagementScore { get; set; }

        public double TimeDecay { get; set; }

        public DateTime CreatedAt { get; set; }

        public int LikeCount { get; set; }

        public int CommentCount { get; set; }

        public int ShareCount { get; set; }
    }

    public class TrendingSummaryDto
    {
        public List<PostTrendDto> Items { get; set; } = new();

        public DateTime GeneratedAt { get; set; }

        public string Scope { get; set; } = "user"; // user | admin
    }
}