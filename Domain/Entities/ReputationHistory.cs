namespace Domain.Entities
{
    public partial class ReputationHistory
    {
        public int ReputationHistoryId { get; set; }

        public int ExpertProfileId { get; set; }

        public int PointChange { get; set; }

        public int CurrentPoint { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ExpertProfile ExpertProfile { get; set; } = null!;
    }
}
