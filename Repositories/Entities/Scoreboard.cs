namespace Repositories.Entities
{
    public partial class Scoreboard
    {
        public int ScoreboardId { get; set; }

        public int PostId { get; set; }

        public int FinalLikeCount { get; set; }

        public int FinalShareCount { get; set; }

        public double ExpertScore { get; set; }

        public string? ExpertReason { get; set; }

        public double CommunityScore { get; set; }

        public double FinalScore { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? Status { get; set; }

        public virtual Post Post { get; set; }
    }
}
