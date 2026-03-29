namespace Repositories.Entities
{
    public partial class PrizeEvent
    {
        public int PrizeEventId { get; set; }

        public int EventId { get; set; }

        public int Ranked { get; set; }

        public decimal RewardAmount { get; set; }

        public int? EscrowSessionId { get; set; }

        public string Status { get; set; }

        public virtual Event Event { get; set; } = null!;

        public virtual ICollection<EventWinner> EventWinners { get; set; } = new List<EventWinner>();
    }
}
