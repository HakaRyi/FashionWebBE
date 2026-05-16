
namespace Domain.Entities
{
    public class PhysicalProfile
    {
        public int Id { get; set; }

        public int AccountId { get; set; }

        public double? Height { get; set; }

        public double? Weight { get; set; }

        public double? Waist { get; set; }

        public double? Hip { get; set; }

        public double? Bust { get; set; }

        public string? BodyShape { get; set; }

        public string? SkinTone { get; set; }

        public DateTime RecordedAt { get; set; }

        public bool IsCurrent { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
