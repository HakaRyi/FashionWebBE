namespace Domain.Entities
{
    public partial class ExpertRating
    {
        public int ExpertRatingId { get; set; }

        public int PostId { get; set; }

        public int ExpertId { get; set; }

        public double Score { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual Post Post { get; set; } = null!;

        public virtual Account Expert { get; set; } = null!;
    }
}
