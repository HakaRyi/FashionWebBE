namespace Domain.Entities
{
    public class PostTrend
    {
        public int PostId { get; set; }

        public double Score { get; set; }

        public double EngagementScore { get; set; }

        public double TimeDecay { get; set; }

        public DateTime CalculatedAt { get; set; }

        public virtual Post Post { get; set; } = null!;
    }
}