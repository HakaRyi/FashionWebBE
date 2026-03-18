namespace Repositories.Entities
{
    public partial class Event
    {
        public int EventId { get; set; }

        public int CreatorId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public double ExpertWeight { get; set; }

        public double UserWeight { get; set; }

        public double PointPerLike { get; set; } = 1;

        public double PointPerShare { get; set; } = 2;

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }

        public virtual Account Creator { get; set; } = null!;

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<PrizeEvent> PrizeEvents { get; set; } = new List<PrizeEvent>();
        public virtual ICollection<EventExpert> EventExperts { get; set; } = new List<EventExpert>();
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
    }
}

