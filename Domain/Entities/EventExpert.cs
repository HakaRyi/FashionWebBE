namespace Domain.Entities
{
    public partial class EventExpert
    {
        public int EventExpertId { get; set; }

        public int EventId { get; set; }

        public int ExpertId { get; set; }

        public DateTime JoinedAt { get; set; }

        public string? Status { get; set; }

        public virtual Event Event { get; set; } = null!;

        public virtual Account Expert { get; set; } = null!;
    }
}
